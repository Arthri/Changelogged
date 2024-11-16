using Changelogged;
using Markdig.Renderers.Roundtrip;
using Markdig.Syntax;
using System.Text.Json;

using var comments = new CommentWriter();

if (args.Length > 1)
{
    comments.Error("Unknown arguments.");
    return 1;
}

if (args.Length < 1)
{
    comments.Error("No subcommand specified.");
    return 1;
}

if (args[0] == "haschangelog")
{
    string? contents = Environment.GetEnvironmentVariable("PULL_REQUEST_BODY");
    if (contents is null)
    {
        comments.Error("PULL_REQUEST_BODY environment variable not set.");
        return 1;
    }

    string? merged = Environment.GetEnvironmentVariable("PULL_REQUEST_MERGED");
    if (merged?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
    {
        comments.Error("Edits are not permitted on merged pull requests.");
    }

    return new PRChangelogVisitor().HasChangelog(contents) ? 0 : 1;
}

if (args[0] == "validate")
{
    string? contents = Environment.GetEnvironmentVariable("PULL_REQUEST_BODY");
    if (contents is null)
    {
        comments.Error("PULL_REQUEST_BODY environment variable not set.");
        return 1;
    }

    new PRValidator(comments).Validate(contents);

    return comments.HasErrors ? 1 : 0;
}

if (args[0] == "build")
{
    string? solutionFilter = Environment.GetEnvironmentVariable("SOLUTION_FILTER");
    if (solutionFilter is null or "")
    {
        comments.Error("SOLUTION_FILTER environment variable not set.");
        return 1;
    }

    string? pullRequestsJSON = Environment.GetEnvironmentVariable("PULL_REQUEST_BODIES");
    if (pullRequestsJSON is null or "")
    {
        comments.Error("PULL_REQUEST_BODIES environment variable not set.");
        return 1;
    }

    List<PullRequest>? pullRequests = JsonSerializer.Deserialize(pullRequestsJSON, SourceGenerationContext.Default.ListPullRequest);
    if (pullRequests is null)
    {
        comments.Error("PULL_REQUEST_BODIES environment variable must not be null.");
        return 1;
    }

    var builder = new ChangelogBuilder(solutionFilter, comments);

    foreach (PullRequest pullRequest in pullRequests)
    {
        comments.Section($"#{pullRequest.Number}");
        builder.Add(pullRequest.Body);
    }

    MarkdownDocument changelog = builder.Build();

    using var stream = new FileStream("changelog.md", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    using var writer = new StreamWriter(stream);
    var renderer = new RoundtripRenderer(writer);
    _ = renderer.Render(changelog);

    return comments.HasErrors ? 1 : 0;
}

comments.Error("Unknown subcommand.");

return 1;

namespace Changelogged;

internal sealed class PullRequest
{
    public required int Number { get; init; }

    public required string Body { get; init; }
}

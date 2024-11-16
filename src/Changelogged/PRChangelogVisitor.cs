using Markdig;
using Markdig.Syntax;

namespace Changelogged;

internal sealed class PRChangelogVisitor : PRVisitor
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of the internal API.")]
    public bool HasChangelog(string markdown)
    {
        MarkdownDocument document = Markdown.Parse(markdown, Options.MarkdownPipeline);

        foreach (Block obj in document)
        {
            if (obj is not HeadingBlock heading)
            {
                continue;
            }

            if (IsChangelogHeading(heading))
            {
                return true;
            }
        }

        return false;
    }
}

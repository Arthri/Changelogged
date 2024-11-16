using Markdig.Renderers;
using Markdig.Syntax;

namespace Changelogged;

internal abstract class PRVisitor
{
    protected static bool IsChangelogHeading(HeadingBlock heading)
    {
        if (heading.Inline is null)
        {
            return false;
        }

        return RenderInPlainText(heading.Inline).Contains("Changelog", StringComparison.OrdinalIgnoreCase);
    }

    protected static bool IsSectionEnd(HeadingBlock sectionHeading, MarkdownObject obj)
    {
        if (obj is HeadingBlock heading)
        {
            // End section if the heading's level is lower(i.e. ## vs ###)
            if (heading.Level <= sectionHeading.Level)
            {
                return true;
            }
        }
        else if (obj is ThematicBreakBlock)
        {
            // End section when encountering horizontal rules
            return true;
        }

        return false;
    }

    protected static string RenderInPlainText(MarkdownObject mdobj)
    {
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            EnableHtmlEscape = false,
            EnableHtmlForBlock = false,
            EnableHtmlForInline = false,
        };

        _ = renderer.Render(mdobj);

        return writer.ToString();
    }
}

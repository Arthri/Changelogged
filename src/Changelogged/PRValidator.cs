using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Changelogged;

internal sealed partial class PRValidator(CommentWriter comments) : PRSectionedVisitor
{
    private HeadingBlock? _changelogHeading;
    private bool _hasChangelog;

    private static readonly IReadOnlySet<string> Projects = ProjectsLoader.FromSolution();

    private void VisitInternal(MarkdownDocument document)
    {
        foreach (Block obj in document)
        {
            VisitSectionElement(obj);

            if (obj is HeadingBlock heading)
            {
                if (IsChangelogHeading(heading))
                {
                    _changelogHeading ??= heading;
                    VisitChangelogHeading(heading);
                }
                else
                {
                    VisitHeading(heading);
                }
            }
        }

        EndSections();

        if (!_hasChangelog)
        {
            comments.Debug("No changelog sections have been detected");
        }
    }

    public void Validate(string markdown)
    {
        MarkdownDocument document = Markdown.Parse(markdown, Options.MarkdownPipeline);

        try
        {
            VisitInternal(document);
        }
        finally
        {
            _changelogHeading = null;
            _hasChangelog = false;
        }
    }

    private void VisitHeading(HeadingBlock heading)
    {
        if (_changelogHeading is null)
        {
            return;
        }

        if (heading.Level == _changelogHeading.Level + 1)
        {
            if (heading.Inline is null)
            {
                comments.Error("Changelog contains empty subheading", heading.Span);
                return;
            }

            if (heading.Inline.Skip(1).Any() || heading.Inline.FirstChild is not LiteralInline)
            {
                comments.Error("Subheadings must be followed by exactly one element: an inline literal containing the project name.", heading.Inline.Span);
            }
            else
            {
                string headingInPlainText = RenderInPlainText(heading.Inline);
                if (!Projects.Contains(headingInPlainText))
                {
                    comments.Error($"Changelog contains unrecognized project `{headingInPlainText}`", heading.Inline.Span);
                }
            }
        }

        if (heading.Level > _changelogHeading.Level + 1)
        {
            comments.Error("Changelogs should not have second-level subheadings", heading.Span);
        }
    }

    private void VisitChangelogHeading(HeadingBlock changelogHeading)
    {
        if (_hasChangelog)
        {
            comments.Error("Multiple changelog sections have been detected.", changelogHeading.Span);

            return;
        }

        Debug.Assert(changelogHeading.Inline is not null, "IsChangelogHeading ensures it is not null");

        if (changelogHeading.Inline.Skip(1).FirstOrDefault() is Inline inline)
        {
            comments.Warn("Inessential content in changelog heading.", inline.Span);
        }

        if (changelogHeading.Inline.FirstChild is not LiteralInline literal)
        {
            Debug.Assert(changelogHeading.Inline.FirstChild is not null, "IsChangelogHeading ensures it is not null");
            comments.Warn("Changelog heading should be a literal.", changelogHeading.Inline.FirstChild.Span);
        }
        else if (literal.Content.Length != "Changelog".Length)
        {
            comments.Warn("Inessential content in changelog heading.", changelogHeading.Inline.Span);
        }

        _hasChangelog = true;
    }

    protected override void VisitSection(Section section)
    {
        if (ReferenceEquals(_changelogHeading, section.Heading))
        {
            _changelogHeading = null;
            VisitChangelogSection(section);
            return;
        }

        if (_changelogHeading is null)
        {
            return;
        }

        if (section.Heading.Level != _changelogHeading.Level + 1)
        {
            // Warnings in other heading levels are handled by VisitHeading
            return;
        }

        if (section.Objects is not [ListBlock list])
        {
            comments.Error("Changelog subsections must have exactly one child: the list of changes.", section.Heading.Span);
            return;
        }

        foreach (Block item in list)
        {
            if (item is not ListItemBlock block)
            {
                comments.Error("The changelog must be solely composed of list items.", item.Span);
                continue;
            }

            if (block.Skip(1).Any() || block.LastChild is not ParagraphBlock paragraph)
            {
                comments.Error("The changelog entries must be composed of exactly one paragraph block.", block.Span);
                continue;
            }

            if (paragraph.Inline is null || paragraph.Inline.FirstChild is not LiteralInline literal)
            {
                comments.Error("The changelog entries must begin with non-empty literal text.", paragraph.Inline?.Span ?? paragraph.Span);
                continue;
            }

            ReadOnlySpan<char> content = literal.Content.AsSpan();
            Match changeTypeMatch = ChangeTypeRegex().Match(content.ToString());
            if (!changeTypeMatch.Success)
            {
                comments.Error("Changes must begin with the change type followed by a colon. For example, `add: new feature`.", literal.Span);
                continue;
            }

            string changeType = changeTypeMatch.Groups[1].Value;
            if (changeType is not ("add" or "feat" or "fix"))
            {
                comments.Error($"Unrecognized change type for {changeType}.", literal.Span);
                continue;
            }
        }
    }

    private void VisitChangelogSection(Section section)
    {
        if (section.Objects is not [HeadingBlock, ..])
        {
            comments.Error("Changelog sections must have at least one element and must begin with a subheading.", section.Heading.Span);
        }
    }

    [GeneratedRegex("""\s*([^:]+):""")]
    private static partial Regex ChangeTypeRegex();
}

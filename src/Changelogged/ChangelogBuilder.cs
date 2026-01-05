using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.ObjectModel;

namespace Changelogged;

internal sealed class ChangelogBuilder(string solutionFilter, CommentWriter comments) : PRSectionedVisitor
{
    private record struct ChangelogSection(List<MarkdownObject> Objects);

    private readonly List<ListItemBlock> _bugFixes = [];
    private readonly List<ListItemBlock> _newFeatures = [];
    private readonly List<ListItemBlock> _otherChanges = [];

    private readonly ReadOnlySet<string> _projects = ProjectsLoader.FromSolutionFilter(solutionFilter);

    private HeadingBlock? _changelogHeading;
    private bool _wasChangelogModified;

    private void AddInternal(MarkdownDocument document)
    {
        foreach (Block obj in document)
        {
            VisitSectionElement(obj);

            if (obj is HeadingBlock heading && IsChangelogHeading(heading))
            {
                if (_changelogHeading is null)
                {
                    _changelogHeading = heading;
                }
                else
                {
                    comments.Error("Multiple changelog headings.", heading.Span);
                    break;
                }
            }
        }

        EndSections();
    }

    public bool Add(string markdown)
    {
        MarkdownDocument document = Markdown.Parse(markdown, Options.TrackedMarkdownPipeline);

        try
        {
            AddInternal(document);
            return _wasChangelogModified;
        }
        finally
        {
            _wasChangelogModified = false;
        }
    }

    protected override void VisitSection(Section section)
    {
        // Warnings handled by PRValidator
        if (ReferenceEquals(_changelogHeading, section.Heading))
        {
            _changelogHeading = null;
            return;
        }

        if (_changelogHeading is null)
        {
            return;
        }

        if (section.Heading.Level != _changelogHeading.Level + 1)
        {
            return;
        }

        if (section.Objects is not [ListBlock list])
        {
            return;
        }

        string headingText = RenderInPlainText(section.Heading);
        if (!_projects.Contains(headingText.Trim()))
        {
            return;
        }

        while (list.Count != 0)
        {
            Block block = list.First();
            if (block is not ListItemBlock { LastChild: ParagraphBlock { Inline: ContainerInline paragraphInline } paragraph } item)
            {
                comments.Error("Invalid changelog entry.", block.Span);
                _ = list.Remove(block);
                continue;
            }

            if (paragraphInline.FirstChild is not LiteralInline literal)
            {
                comments.Error("Changelog entries must start with literal text.", paragraphInline.Span);
                _ = list.Remove(block);
                continue;
            }

            ReadOnlySpan<char> content = literal.Content.AsSpan();
            int indexOfColon = content.IndexOf(':');
            string changeType = content[..indexOfColon].TrimStart().ToString();
            List<ListItemBlock> changelog;
            switch (changeType)
            {
                case "fix":
                {
                    changelog = _bugFixes;
                    break;
                }
                case "add":
                {
                    changelog = _newFeatures;
                    break;
                }
                case "feat":
                {
                    changelog = _otherChanges;
                    break;
                }
                default:
                {
                    comments.Error($"Unrecognized change type {changeType}", literal.Span);
                    _ = list.Remove(block);
                    continue;
                }
            }

            ReadOnlySpan<char> newContent = content[(indexOfColon + 1)..].TrimStart();
            literal.Content = new StringSlice(literal.Content.Text, literal.Content.End - newContent.Length, literal.Content.End, literal.Content.NewLine);

            if (paragraphInline.LastChild is LineBreakInline lineBreak)
            {
                lineBreak.NewLine = NewLine.LineFeed;
            }
            else
            {
                _ = paragraphInline.AppendChild(new LineBreakInline
                {
                    NewLine = NewLine.LineFeed,
                });
            }

            item.LinesBefore = null;
            item.LinesAfter = null;
            paragraph.LinesBefore = null;
            paragraph.LinesAfter = null;
            _ = list.Remove(block);
            changelog.Add(item);
            _wasChangelogModified = true;
        }
    }

    public MarkdownDocument Build()
    {
        var document = new MarkdownDocument();
        var renderCache = new Dictionary<MarkdownObject, string>();

        TryAddChangelog(" 🆕 New Features", _newFeatures);
        TryAddChangelog(" 🐞 Bug Fixes", _bugFixes);
        TryAddChangelog(" 🛠 Other Changes", _otherChanges);

        return document;

        void TryAddChangelog(string heading, List<ListItemBlock> changelogEntries)
        {
            if (changelogEntries.Count == 0)
            {
                return;
            }

            document.Add(CreateHeading(heading));

            var changelog = new ListBlock(null!)
            {
                BulletType = '-',
            };
            document.Add(changelog);

            changelogEntries.Sort((a, b) => string.CompareOrdinal(GetOrRender(a), GetOrRender(b)));
            foreach (ListItemBlock entry in changelogEntries)
            {
                changelog.Add(entry);
            }
        }

        string GetOrRender(MarkdownObject obj)
        {
            if (renderCache.TryGetValue(obj, out string? cached))
            {
                return cached;
            }

            return renderCache[obj] = RenderInPlainText(obj);
        }
    }

    private static HeadingBlock CreateHeading(string text)
    {
        var container = new ContainerInline();
        _ = container.AppendChild(new LiteralInline(text));

        return new HeadingBlock(null!)
        {
            Inline = container,
            Level = 2,
            TriviaBefore = new StringSlice("\n"),
            TriviaAfter = new StringSlice("\n"),
        };
    }
}

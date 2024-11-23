using Markdig.Syntax;

namespace Changelogged;

internal class PRSectionedVisitor : PRVisitor
{
    protected readonly record struct Section(HeadingBlock Heading, List<MarkdownObject> Objects);

    private readonly Stack<Section> _sections = new(6);

    protected void VisitSectionElement(MarkdownObject obj)
    {
        while (_sections.Count > 0 && IsSectionEnd(_sections.Peek().Heading, obj))
        {
            VisitSection(_sections.Pop());
        }

        foreach (Section section in _sections)
        {
            section.Objects.Add(obj);
        }

        if (obj is HeadingBlock heading)
        {
            var list = new List<MarkdownObject>();
            _sections.Push(new Section(heading, list));
        }
    }

    protected void EndSections()
    {
        while (_sections.Count > 0)
        {
            VisitSection(_sections.Pop());
        }
    }

    protected virtual void VisitSection(Section section)
    {
    }
}

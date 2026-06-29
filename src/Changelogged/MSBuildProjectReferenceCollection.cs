using System.Collections;

namespace Changelogged;

internal sealed class MSBuildProjectReferenceCollection : IReadOnlyList<MSBuildProjectReference>
{
    public MSBuildProjectReference this[int index] => Items[index];

    public IReadOnlyList<MSBuildProjectReference> Items { get; init; } = null!;

    public int Count => Items.Count;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    public IEnumerator<MSBuildProjectReference> GetEnumerator()
    {
        return Items.GetEnumerator();
    }
}

namespace Changelogged;

internal sealed class MSBuildEvaluationOutput
{
    internal sealed class MSBuildItems
    {
        public IReadOnlyList<MSBuildProjectReference> ProjectReference { get; set; } = null!;
    }

    public MSBuildItems Items { get; set; } = null!;
}

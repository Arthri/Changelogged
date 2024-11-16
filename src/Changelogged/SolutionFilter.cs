namespace Changelogged;

internal sealed class SolutionFilter
{
    internal sealed class SLNFSolution
    {
        public required string Path { get; init; }

        public required IReadOnlyList<string> Projects { get; init; }
    }

    public required SLNFSolution Solution { get; init; }
}

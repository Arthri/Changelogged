using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace Changelogged;

internal static class Options
{
    internal static MarkdownPipeline MarkdownPipeline { get; } = CreatePipeline()
        .Build()
    ;

    internal static MarkdownPipeline TrackedMarkdownPipeline { get; } = CreatePipeline()
        .EnableTrackTrivia()
        .Build()
    ;

    private static MarkdownPipelineBuilder CreatePipeline()
    {
        return new MarkdownPipelineBuilder()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .UsePreciseSourceLocation()
        ;
    }
}

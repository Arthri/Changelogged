using System.Text.Json.Serialization;

namespace Changelogged;

[JsonSerializable(typeof(PullRequest))]
[JsonSerializable(typeof(List<PullRequest>))]
[JsonSerializable(typeof(MSBuildProjectReference))]
[JsonSerializable(typeof(MSBuildEvaluationOutput))]
[JsonSerializable(typeof(MSBuildEvaluationOutput.MSBuildItems))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext;

using System.Text.Json.Serialization;

namespace Changelogged;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(PullRequest))]
[JsonSerializable(typeof(List<PullRequest>))]
[JsonSerializable(typeof(MSBuildProjectReference))]
[JsonSerializable(typeof(MSBuildProjectReferenceCollection))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext;

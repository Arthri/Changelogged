using System.Text.Json.Serialization;

namespace Changelogged;

internal sealed class PullRequest
{
    [JsonPropertyName("number")]
    public required int Number { get; init; }

    [JsonPropertyName("body")]
    public required string? Body { get; init; }
}

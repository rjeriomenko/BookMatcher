using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses.Llm;

// response schema from LLM ranking and matching candidate works to hypotheses
// Semantic Kernel generates a JSON schema from this record, which is sent as part of the Request to the LLM APIs
// the JSON schema constrains the output of the LLMs to match the JSON schema
// this record is ALSO used to deserialize that constrained response from the LLMs
public record LlmRankedMatchResponseSchema
{
    [JsonPropertyName("matches")]
    [Required]
    public required List<LlmRankedMatch> Matches { get; init; }
}

// LLM-selected work key with explanation, ordered by best match
public record LlmRankedMatch
{
    [JsonPropertyName("work_key")]
    [Required]
    public required string WorkKey { get; init; }

    [JsonPropertyName("primary_authors")]
    [Required]
    public required List<string> PrimaryAuthors { get; init; }

    [JsonPropertyName("contributors")]
    public List<string> Contributors { get; init; } = [];

    [JsonPropertyName("reasoning")]
    [Required]
    [MaxLength(500)]
    public required string Reasoning { get; init; }
}

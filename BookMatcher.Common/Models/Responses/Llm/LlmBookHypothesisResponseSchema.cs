using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses.Llm;

// response schema from LLM extracting hypotheses from the user's messy query
// Semantic Kernel generates a JSON schema from this record, which is sent as part of the Request to the LLM APIs 
// the JSON schema constrains the output of the LLMs to match the JSON schema
// this record is ALSO used to deserialize that constrained response from the LLMs
public record LlmBookHypothesisResponseSchema
{
    [JsonPropertyName("hypotheses")] 
    [Required]
    public required List<LlmBookHypothesis> Hypotheses { get; init; }
}

// individual book hypothesis extracted from user's messy query
public record LlmBookHypothesis
{
    [JsonPropertyName("title")]
    [MaxLength(500)]
    public string? Title { get; init; }
    
    [JsonPropertyName("author")]
    [MaxLength(500)]
    public string? Author { get; init; }
    
    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; init; }
    
    [JsonPropertyName("confidence")]
    [Required]
    public required int Confidence { get; init; }
    
    [JsonPropertyName("reasoning")]
    [Required]
    [MaxLength(500)]
    public required string Reasoning { get; init; }
}
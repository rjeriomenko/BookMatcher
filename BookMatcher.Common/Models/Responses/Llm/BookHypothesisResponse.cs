using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses.Llm;

// response from LLM ExtractHypothesesAsync
public record BookHypothesisResponse
{
    [JsonPropertyName("hypotheses")] 
    [Required]
    public required List<BookHypothesis> Hypotheses { get; init; }
}

// individual book hypothesis extracted from user's messy query
public record BookHypothesis
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }
    
    [JsonPropertyName("author")]
    public string? Author { get; init; }
    
    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; init; }
    
    [JsonPropertyName("confidence")]
    [Required]
    public required int Confidence { get; init; }
    
    [JsonPropertyName("reasoning")]
    [Required]
    public required string Reasoning { get; init; }
}
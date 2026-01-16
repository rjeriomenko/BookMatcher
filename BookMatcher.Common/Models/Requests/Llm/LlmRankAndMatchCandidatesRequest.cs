using System.Text.Json.Serialization;
using BookMatcher.Common.Models.Responses.Llm;
using BookMatcher.Common.Models.Responses.OpenLibrary;

namespace BookMatcher.Common.Models.Requests.Llm;

// request for LLM to rank and match candidate works to hypotheses
public record LlmRankAndMatchCandidatesRequest
{
    [JsonPropertyName("original_query")]
    public required string OriginalQuery { get; init; }

    [JsonPropertyName("hypotheses_with_candidate_works")]
    public required List<LlmBookHypothesisWithCandidateWorks> HypothesesWithCandidateWorks { get; init; }
}

// groups a book hypothesis with its candidate works from OpenLibrary search
public record LlmBookHypothesisWithCandidateWorks
{
    public required LlmBookHypothesis LlmBookHypothesis { get; init; }
    public required List<OpenLibraryWorkDocument> CandidateWorks { get; init; }
}

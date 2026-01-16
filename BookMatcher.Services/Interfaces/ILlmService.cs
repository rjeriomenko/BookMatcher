using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Requests.Llm;
using BookMatcher.Common.Models.Responses.Llm;

namespace BookMatcher.Services.Interfaces;

public interface ILlmService
{
    // extract up to 5 book hypotheses from user's messy query using LLM
    Task<LlmBookHypothesisResponseSchema> ExtractLlmBookMatchHypothesesAsync(
        string blob,
        LlmModel? model = null,
        float? temperature = null,
        int? maxTokens = null);

    // rank and match candidate works to hypotheses, returning best matches in order using LLM
    // ordered by strength of match to the user's original query
    Task<LlmRankedMatchResponseSchema> RankCandidatesAsync(
        LlmRankAndMatchCandidatesRequest request,
        LlmModel? model = null,
        float? temperature = null,
        int? maxTokens = null);
}
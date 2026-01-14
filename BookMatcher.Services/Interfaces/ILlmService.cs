using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Responses.Llm;

namespace BookMatcher.Services.Interfaces;

public interface ILlmService
{
    // extract up to 5 book hypotheses from user's messy query using LLM
    Task<BookHypothesisResponse> ExtractHypothesesAsync(
        string blob,
        LlmModel? model = null,
        float? temperature = null,
        int? maxTokens = null);
}
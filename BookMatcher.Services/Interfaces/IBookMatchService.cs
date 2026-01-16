using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Responses;

namespace BookMatcher.Services.Interfaces;

public interface IBookMatchService
{
    // find best matching books for user's messy query using LLM hypotheses, multi-stage OpenLibrary API search, and LLM ranking
    Task<List<BookMatch>> FindBookMatchesAsync(string query, LlmModel? model = null);
}

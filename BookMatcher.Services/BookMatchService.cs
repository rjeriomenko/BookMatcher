using System.Text.RegularExpressions;
using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Requests.Llm;
using BookMatcher.Common.Models.Responses;
using BookMatcher.Common.Models.Responses.Llm;
using BookMatcher.Common.Models.Responses.OpenLibrary;
using BookMatcher.Services.Interfaces;

namespace BookMatcher.Services;

public partial class BookMatchService : IBookMatchService
{
    private readonly ILlmService _llmService;
    private readonly IOpenLibraryService _openLibraryService;

    public BookMatchService(ILlmService llmService, IOpenLibraryService openLibraryService)
    {
        _llmService = llmService;
        _openLibraryService = openLibraryService;
    }

    // normalize text for API search
    // lowercase, trim, strip special chars except spaces
    private string NormalizeForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // convert to lowercase and remove special characters except spaces
        var normalized = SpecialCharRegex().Replace(input.ToLowerInvariant(), " ");

        // collapse multiple spaces and trim
        return MultipleSpacesRegex().Replace(normalized, " ").Trim();
    }
    
    // generate cover URL from work key
    private string? GenerateCoverUrl(string workKey)
    {
        // work key format: /works/OL45883W
        // cover URL format: https://covers.openlibrary.org/b/olid/OL45883W-L.jpg
        var workId = workKey.Split('/').LastOrDefault();
        return !string.IsNullOrEmpty(workId)
            ? $"https://covers.openlibrary.org/b/olid/{workId}-L.jpg"
            : null;
    }

    // find book match candidates for a single hypothesis using multi-stage OpenLibrary API search
    // OpenLibrary's API returns works as documents sorted by relevance
    private async Task<List<OpenLibraryWorkDocumentResponse>> FindMatchCandidatesForHypothesis(LlmBookHypothesis hypothesis)
    {
        var candidateWorkDocuments = new List<OpenLibraryWorkDocumentResponse>();

        // normalize all search fields
        var normalizedTitle = NormalizeForSearch(hypothesis.Title);
        var normalizedAuthor = NormalizeForSearch(hypothesis.Author);
        var normalizedKeywords = hypothesis.Keywords?
            .Select(NormalizeForSearch)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();
        var normalizedJoinedKeywords = normalizedKeywords is { Count: > 0 }
            ? string.Join(" ", normalizedKeywords)
            : null;

        // first, perform a precise search with all search fields (q, title, author)
        // tends to return either close matches or no matches
        var preciseSearchResult = await _openLibraryService.SearchAsync(
            query: normalizedJoinedKeywords,
            title: normalizedTitle,
            author: normalizedAuthor,
            limit: 5);

        if (preciseSearchResult?.Docs is { Count: > 0 })
            candidateWorkDocuments.AddRange(preciseSearchResult.Docs);

        // next, perform a broader search with just title and author fields if we have zero total results
        // tends to return more results but weaker matches
        if (candidateWorkDocuments.Count == 0 &&
            (!string.IsNullOrEmpty(normalizedTitle) || !string.IsNullOrEmpty(normalizedAuthor)))
        {
            var titleAuthorSearchResult = await _openLibraryService.SearchAsync(
                query: null,
                title: normalizedTitle,
                author: normalizedAuthor,
                limit: 5);

            if (titleAuthorSearchResult?.Docs is { Count: > 0 })
                candidateWorkDocuments.AddRange(titleAuthorSearchResult.Docs);
        }

        // finally perform a keywords-only search if we still have less than 5 total results
        // append these results to total results
        // tends to return even more results but with the weakest matches
        if (candidateWorkDocuments.Count < 5 && !string.IsNullOrEmpty(normalizedJoinedKeywords))
        {
            var keywordsSearchResult = await _openLibraryService.SearchAsync(
                query: normalizedJoinedKeywords,
                title: null,
                author: null,
                limit: 5 - candidateWorkDocuments.Count);

            if (keywordsSearchResult?.Docs is { Count: > 0 })
                candidateWorkDocuments.AddRange(keywordsSearchResult.Docs);
        }

        // de-duplicate by grouping by OpenLibrary key
        return candidateWorkDocuments
            .Where(doc => !string.IsNullOrEmpty(doc.Key))
            .GroupBy(doc => doc.Key)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<List<BookMatch>> FindBookMatchesAsync(string query, LlmModel? model = null)
    {
        // step 1: extract book match hypotheses from user query using LLM
        // turns a user's messy query into hypotheses for what book that user is searching for
        var bookMatchHypothesesResponse = await _llmService.ExtractLlmBookMatchHypothesesAsync(query, model);
        if (bookMatchHypothesesResponse.Hypotheses.Count == 0)
            return [];

        // step 2: find book match candidates for each hypothesis using multiple OpenLibrary API queries in parallel
        // uses the OpenLibrary API to find real works that are candidates for matching with each LLM hypotheses
        var candidateWorksTasks = bookMatchHypothesesResponse.Hypotheses
            .Select(async hypothesis => new
            {
                Hypothesis = hypothesis,
                Candidates = await FindMatchCandidatesForHypothesis(hypothesis)
            })
            .ToList();
        var candidateWorksResults = await Task.WhenAll(candidateWorksTasks);

        var hypothesesWithCandidateWorks = candidateWorksResults
            .Where(r => r.Candidates.Count > 0)
            .Select(r => new LlmBookHypothesisWithCandidateWorks
            {
                LlmBookHypothesis = r.Hypothesis,
                CandidateWorks = r.Candidates
            })
            .ToList();
        if (hypothesesWithCandidateWorks.Count == 0)
            return [];

        // step 3: ask LLM to rank and match candidate works to hypotheses
        // returns real works ordered by strength of match to the user's original query
        var llmRankAndMatchCandidatesRequest = new LlmRankAndMatchCandidatesRequest
        {
            OriginalQuery = query,
            HypothesesWithCandidateWorks = hypothesesWithCandidateWorks
        };

        var rankedResponse = await _llmService.RankCandidatesAsync(llmRankAndMatchCandidatesRequest, model);

        // step 4: convert response to List<BookMatch> with cover URL and OpenLibrary page URL
        // returns ordered list with metadata and LLM reasoning
        return rankedResponse.Matches.Select(m => new BookMatch
        {
            Title = m.Title,
            PrimaryAuthor = m.PrimaryAuthor,
            Contributors = m.Contributors,
            FirstPublishYear = m.FirstPublishYear,
            WorkKey = m.WorkKey,
            CoverUrl = GenerateCoverUrl(m.WorkKey),
            OpenLibraryUrl = $"https://openlibrary.org{m.WorkKey}",
            Explanation = m.Explanation
        }).ToList();
    }

    // use source generator for optimized regex implementation at compile time
    [GeneratedRegex(@"[^a-z0-9\s]")]
    private static partial Regex SpecialCharRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();
}

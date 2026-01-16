using BookMatcher.Common.Models.Responses.OpenLibrary;

namespace BookMatcher.Services.Interfaces;

public interface IOpenLibraryService
{
    // search OpenLibrary Search API with query parameters
    public Task<OpenLibraryWorkSearchResponse?> SearchAsync(
        string? query = null,
        string? title = null,
        string? author = null,
        int limit = 10);

    // get the first edition key for a work using the editions field
    public Task<string?> GetFirstEditionKeyAsync(string workKey);
}
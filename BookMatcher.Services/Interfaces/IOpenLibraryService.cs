using BookMatcher.Common.Models.Responses.OpenLibrary;

namespace BookMatcher.Services.Interfaces;

public interface IOpenLibraryService
{
    // search OpenLibrary Search API with query parameters
    public Task<OpenLibrarySearchResponse?> SearchAsync(string? query = null, string? title = null, string? author = null, int limit = 10);
}
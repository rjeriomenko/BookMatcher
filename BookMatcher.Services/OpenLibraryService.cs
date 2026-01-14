using System.Text.Json;
using BookMatcher.Common.Models.Configurations;
using BookMatcher.Common.Models.Responses.OpenLibrary;
using BookMatcher.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookMatcher.Services;

public class OpenLibraryService : IOpenLibraryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenLibraryConfiguration _config;

    public OpenLibraryService(IHttpClientFactory httpClientFactory, IOptions<OpenLibraryConfiguration> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
    }

    private HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient(nameof(OpenLibraryService));
    }

    public async Task<OpenLibrarySearchResponse?> SearchAsync(string? query = null, string? title = null, string? author = null, int limit = 10)
    {
        // build query string with provided parameters
        var client = _httpClientFactory.CreateClient(nameof(OpenLibraryService));
        var queryString = new QueryString();

        if (!string.IsNullOrWhiteSpace(query))
            queryString = queryString.Add("q", query);
        if (!string.IsNullOrWhiteSpace(title))
            queryString = queryString.Add("title", title);
        if (!string.IsNullOrWhiteSpace(author))
            queryString = queryString.Add("author", author);
        queryString = queryString.Add("limit", limit.ToString());

        try
        {
            var response = await client.GetAsync($"/search.json{queryString}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<OpenLibrarySearchResponse>(content);

            return searchResponse;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
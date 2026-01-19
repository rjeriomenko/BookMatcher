using System.Text.Json;
using BookMatcher.Common.Exceptions;
using BookMatcher.Common.Models.Configurations;
using BookMatcher.Common.Models.Responses.OpenLibrary;
using BookMatcher.Services.Interfaces;
using Microsoft.AspNetCore.Http;
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

    public async Task<OpenLibraryWorkSearchResponse?> SearchAsync(string? query = null, string? title = null, string? author = null, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query) &&
            string.IsNullOrWhiteSpace(title) &&
            string.IsNullOrWhiteSpace(author))
            return null;
        
        // build query string with provided parameters
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
            var httpResponse = await CreateHttpClient().GetAsync($"/search.json{queryString}");
            httpResponse.EnsureSuccessStatusCode();

            var content = await httpResponse.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<OpenLibraryWorkSearchResponse>(content);

            return searchResponse;
        }
        catch (HttpRequestException ex)
        {
            throw new OpenLibraryServiceException($"OpenLibrary API request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new OpenLibraryServiceException($"Failed to deserialize OpenLibrary response: {ex.Message}", ex);
        }
    }

    public async Task<string?> GetFirstEditionKeyAsync(string workKey)
    {
        if (string.IsNullOrWhiteSpace(workKey))
            return null;

        // build query string to search for this specific work and include editions in the response
        var queryString = new QueryString()
            .Add("q", $"key:{workKey}")
            .Add("fields", "key,editions")
            .Add("limit", "1");

        try
        {
            var httpResponse = await CreateHttpClient().GetAsync($"/search.json{queryString}");
            httpResponse.EnsureSuccessStatusCode();

            var content = await httpResponse.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<OpenLibraryWorkSearchResponse>(content);

            // extract the first edition key from the first work document
            var firstEditionKey = searchResponse?.Docs?
                .FirstOrDefault()?
                .Editions?.Docs?
                .FirstOrDefault()?
                .Key;

            return firstEditionKey;
        }
        catch (HttpRequestException ex)
        {
            throw new OpenLibraryServiceException($"OpenLibrary API request failed when fetching edition: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new OpenLibraryServiceException($"Failed to deserialize OpenLibrary edition response: {ex.Message}", ex);
        }
    }
}
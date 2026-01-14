using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses.OpenLibrary;

// response from OpenLibrary /search.json API endpoint
// JsonPropertyName decorators ensure that the response is correctly serialized from JSON
public record OpenLibrarySearchResponse
{
    [JsonPropertyName("start")] public int Start { get; init; }
    
    [JsonPropertyName("numFound")] public int NumFound { get; init; }

    [JsonPropertyName("docs")] public List<OpenLibraryDocumentResponse> Docs { get; init; } = [];
}
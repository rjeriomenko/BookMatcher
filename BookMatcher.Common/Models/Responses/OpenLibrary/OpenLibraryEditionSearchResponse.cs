using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses.OpenLibrary;

// nested editions response when using fields=editions in OpenLibrary API search query
public record OpenLibraryEditionSearchResponse
{
    [JsonPropertyName("numFound")] public int NumFound { get; init; }

    [JsonPropertyName("docs")] public List<OpenLibraryEditionDocument> Docs { get; init; } = [];
}

// individual edition document within editions response
public record OpenLibraryEditionDocument
{
    [JsonPropertyName("key")] public string? Key { get; init; }

    [JsonPropertyName("title")] public string? Title { get; init; }

    [JsonPropertyName("cover_i")] public int? CoverId { get; init; }

    [JsonPropertyName("author_name")] public List<string>? AuthorName { get; init; }

    [JsonPropertyName("publish_date")] public List<string>? PublishDate { get; init; }
}
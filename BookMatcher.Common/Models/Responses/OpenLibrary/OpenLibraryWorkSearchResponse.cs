using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses.OpenLibrary;

// response from OpenLibrary /search.json API endpoint
// JsonPropertyName decorators ensure that the response is correctly serialized from JSON
public record OpenLibraryWorkSearchResponse
{
    [JsonPropertyName("start")] public int Start { get; init; }

    [JsonPropertyName("numFound")] public int NumFound { get; init; }

    [JsonPropertyName("docs")] public List<OpenLibraryWorkDocument> Docs { get; init; } = [];
}

// schema for individual book (work) from OpenLibrary API
// schema is not guaranteed to be stable
// JsonPropertyName decorators ensure that the response is correctly serialized from JSON
public record OpenLibraryWorkDocument
{
    [JsonPropertyName("cover_i")] public int? CoverId { get; init; }

    [JsonPropertyName("has_fulltext")] public bool? HasFulltext { get; init; }

    [JsonPropertyName("edition_count")] public int? EditionCount { get; init; }

    [JsonPropertyName("title")] public string? Title { get; init; }

    // parallel arrays - index i in author_name corresponds to index i in author_key
    [JsonPropertyName("author_name")] public List<string>? AuthorName { get; init; }

    [JsonPropertyName("author_key")] public List<string>? AuthorKey { get; init; }

    [JsonPropertyName("first_publish_year")] public int? FirstPublishYear { get; init; }

    [JsonPropertyName("key")] public string? Key { get; init; }

    [JsonPropertyName("ia")] public List<string>? Ia { get; init; }

    [JsonPropertyName("language")] public List<string>? Language { get; init; }

    [JsonPropertyName("editions")] public OpenLibraryEditionSearchResponse? Editions { get; init; }
}
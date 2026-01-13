using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses;

public record BookMatchResponse
{
    // JsonPropertyName decorators ensure that the response is serialized into snake_case JSON
    [JsonPropertyName("title")] public required string Title { get; init; }

    [JsonPropertyName("author")] public required string Author { get; init; }

    [JsonPropertyName("first_publish_year")] public int? FirstPublishYear { get; init; }

    [JsonPropertyName("open_library_id")] public string? OpenLibraryId { get; init; }

    [JsonPropertyName("open_library_url")] public string? OpenLibraryUrl { get; init; }

    [JsonPropertyName("cover_image_url")] public string? CoverImageUrl { get; init; }

    [JsonPropertyName("explanation")] public required string Explanation { get; init; }
}
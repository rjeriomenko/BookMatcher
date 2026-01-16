using System.Text.Json.Serialization;

namespace BookMatcher.Common.Models.Responses;

// response from BookMatcher /match API endpoint
// list ordered by best match by LLM
public record BookMatchResponse
{
    [JsonPropertyName("matches")]
    public required List<BookMatch> Matches { get; init; }
}

// book match with metadata
public record BookMatch
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("primary_authors")]
    public required List<string> PrimaryAuthors { get; init; }

    [JsonPropertyName("contributors")]
    public List<string> Contributors { get; init; } = [];

    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; init; }

    [JsonPropertyName("work_key")]
    public required string WorkKey { get; init; }

    [JsonPropertyName("cover_url")]
    public string? CoverUrl { get; init; }

    [JsonPropertyName("open_library_url")]
    public required string OpenLibraryUrl { get; init; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; init; }
}
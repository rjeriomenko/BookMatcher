using System.ComponentModel.DataAnnotations;

namespace BookMatcher.Common.Models.Configurations;

public class OpenLibraryConfiguration
{
    [Required] public string ApiBaseUrl { get; init; } = null!;
}
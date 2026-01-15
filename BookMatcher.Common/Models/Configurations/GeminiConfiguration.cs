using System.ComponentModel.DataAnnotations;

namespace BookMatcher.Common.Models.Configurations;

public class GeminiConfiguration
{
    [Required] public string ApiKey { get; init; } = null!;
    [Required] public string FlashLiteModel { get; init; } = null!;
    [Required] public string FlashModel { get; init; } = null!;
}
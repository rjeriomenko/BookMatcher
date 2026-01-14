using System.ComponentModel.DataAnnotations;

namespace BookMatcher.Common.Models.Configurations;

public class OpenAiConfiguration
{
    [Required] public string ApiKey { get; init; } = null!;
    [Required] public string NanoModel { get; init; } = null!;
}
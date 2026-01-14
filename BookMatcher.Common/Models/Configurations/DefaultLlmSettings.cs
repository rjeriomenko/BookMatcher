using System.ComponentModel.DataAnnotations;

namespace BookMatcher.Common.Models.Configurations;

public class DefaultLlmSettings
{
    [Required] [Range(0.0, 2.0)] public float Temperature { get; init; }
    [Required] [Range(1, 10000)] public int MaxTokens { get; init; }
}
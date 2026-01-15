using System.Text.Json;
using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Configurations;
using BookMatcher.Common.Models.Responses.Llm;
using BookMatcher.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// required to use experimental SK connectors (Google)
#pragma warning disable SKEXP0070

namespace BookMatcher.Services;

public class LlmService : ILlmService
{
    private readonly Kernel _kernel;
    private readonly DefaultLlmSettings _defaultSettings;

    public LlmService(
        Kernel kernel,
        IOptions<DefaultLlmSettings> defaultSettings)
    {
        _kernel = kernel;
        _defaultSettings = defaultSettings.Value;
    }

    // get the service id for the llm model that the kernel wil use to instantiate the ChatCompletionService
    // the service id is an internal id -- not to be confused with official model designations ("gemini-2.5-flash-lite")
    private string GetServiceId(LlmModel model)
    {
        return model switch
        {
            LlmModel.GeminiFlash => "gemini-flash",
            LlmModel.GeminiPro => "gemini-pro",
            LlmModel.GptNano => "gpt-nano",
            _ => "gemini-flash"
        };
    }

    // get the PromptExecutionSettings for the chosen llm model
    private PromptExecutionSettings GetPromptExecutionSettings(LlmModel model, float temperature, int maxTokens)
    {
        return model switch
        {
            LlmModel.GeminiFlash or LlmModel.GeminiPro => new GeminiPromptExecutionSettings
            {
                Temperature = temperature,
                MaxTokens = maxTokens,
                ResponseSchema = typeof(BookHypothesisResponse),
                ResponseMimeType = "application/json"
            },
            LlmModel.GptNano => new OpenAIPromptExecutionSettings
            {
            // openai nano model only supports temperature of 1
                Temperature = 1,
                MaxTokens = maxTokens,
                ResponseFormat = typeof(BookHypothesisResponse)
            },
            _ => new GeminiPromptExecutionSettings
            {
                Temperature = temperature,
                MaxTokens = maxTokens,
                ResponseSchema = typeof(BookHypothesisResponse),
                ResponseMimeType = "application/json"
            }
        };
    }

    public async Task<BookHypothesisResponse> ExtractHypothesesAsync(
        string blob,
        LlmModel? model = null,
        float? temperature = null,
        int? maxTokens = null)
    {
        var selectedModel = model ?? LlmModel.GeminiFlash;
        var serviceId = GetServiceId(selectedModel);
        var settings = GetPromptExecutionSettings(
            selectedModel,
            temperature ?? _defaultSettings.Temperature,
            maxTokens ?? _defaultSettings.MaxTokens);

        var chatService = _kernel.GetRequiredService<IChatCompletionService>(serviceId);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            "You are a book search expert. Extract 0-5 book hypotheses from the user's messy query. " +
            "Normalize all title and author fields for API search: remove subtitle separators (colons, dashes), strip punctuation, use standard spellings, and avoid special characters. " +
            "Use the following hierarchy of match criteria to justify your hypotheses (in order of strongest to weakest match criteria): " +
            "[1. Exact/normalized title + primary author match (strongest match), 2. Exact/normalized title + contributor-only author (lower rank), 3. Near-match title + author match (candidate), 4. Author-only-match (fallback criteria)] " +
            "For each hypothesis, if the match is stronger than the fallback criteria, provide at least one of: title, author, or keywords (1-5 relevant search terms). " +
            "If the hypothesis for the match relies on fallback criteria (exact or near author-only-match), default to a hypothesis for a top work by that author. " +
            "Additionally provide: confidence (1-5 integer, where 5 is highest), and reasoning (1-2 sentences explaining why this book matches). " +
            "List the hypotheses in order of descending confidence. " +
            "Do not provide duplicate hypotheses for the same book or series. " +
            "Do not extract hypotheses with insufficient information (e.g., only a single generic keyword). " +
            "Example response format: {\"hypotheses\": [{\"title\": \"The Hobbit\", \"author\": \"JRR Tolkien\", \"keywords\": [\"hobbit\", \"fantasy\"], \"confidence\": 5, \"reasoning\": \"Exact title and author match from user query.\"}]}");
        chatHistory.AddUserMessage(blob);

        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel);

        var result = JsonSerializer.Deserialize<BookHypothesisResponse>(response.Content!);
        return result ?? new BookHypothesisResponse { Hypotheses = [] };
    }
}

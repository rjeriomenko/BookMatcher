using System.Text.Json;
using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Configurations;
using BookMatcher.Common.Models.Requests.Llm;
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

    public LlmService(Kernel kernel, IOptions<DefaultLlmSettings> defaultSettings)
    {
        _kernel = kernel;
        _defaultSettings = defaultSettings.Value;
    }

    // get the service id for the LLM model that the kernel wil use to instantiate the ChatCompletionService
    // the service id is an internal id -- not to be confused with official model designations ("gemini-2.5-flash-lite")
    private string GetServiceId(LlmModel model)
    {
        return model switch
        {
            LlmModel.GeminiFlashLite => "gemini-flash-lite",
            LlmModel.GeminiFlash => "gemini-flash",
            LlmModel.GptNano => "gpt-nano",
            _ => "gemini-flash"
        };
    }

    // get the PromptExecutionSettings for the chosen LLM model with specified response schema
    private PromptExecutionSettings GetPromptExecutionSettings(LlmModel model, float temperature, int maxTokens, Type responseSchemaType)
    {
        return model switch
        {
            LlmModel.GeminiFlashLite or LlmModel.GeminiFlash => new GeminiPromptExecutionSettings
            {
                Temperature = temperature,
                MaxTokens = maxTokens,
                ResponseSchema = responseSchemaType,
                ResponseMimeType = "application/json"
            },
            LlmModel.GptNano => new OpenAIPromptExecutionSettings
            {
                // openai nano model only supports temperature of 1
                Temperature = 1,
                MaxTokens = maxTokens,
                ResponseFormat = responseSchemaType
            },
            _ => new GeminiPromptExecutionSettings
            {
                Temperature = temperature,
                MaxTokens = maxTokens,
                ResponseSchema = responseSchemaType,
                ResponseMimeType = "application/json"
            }
        };
    }

    // create configured chat completion service with model selection and settings
    private (IChatCompletionService chatCompletionService, PromptExecutionSettings settings) CreateConfiguredChatCompletionService(
        LlmModel? model,
        float? temperature,
        int? maxTokens,
        Type responseSchemaType)
    {
        var selectedModel = model ?? LlmModel.GeminiFlashLite;
        var serviceId = GetServiceId(selectedModel);
        
        var settings = GetPromptExecutionSettings(
            selectedModel,
            temperature ?? _defaultSettings.Temperature,
            maxTokens ?? _defaultSettings.MaxTokens,
            responseSchemaType);

        var chatService = _kernel.GetRequiredService<IChatCompletionService>(serviceId);
        
        return (chatService, settings);
    }

    public async Task<LlmBookHypothesisResponseSchema> ExtractLlmBookMatchHypothesesAsync(
        string blob,
        LlmModel? model = null,
        float? temperature = null,
        int? maxTokens = null)
    {
        var (chatCompletionService, settings) = CreateConfiguredChatCompletionService(
            model,
            temperature,
            maxTokens,
            typeof(LlmBookHypothesisResponseSchema));

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            "You are a book search expert. Extract 0-5 book hypotheses from the user's messy query. " +
            "Normalize all title and author fields for API search: remove subtitle separators (colons, dashes), strip punctuation and diacritics, use standard spellings, use lowercase only, avoid special characters, and put spaces between an author's initials. " +
            "Use the following hierarchy of match criteria to justify your hypotheses (in order of strongest to weakest match criteria): " +
            "[1. Exact/normalized title + primary author match (strongest match), 2. Exact/normalized title + contributor-only author (lower rank), 3. Near-match title + author match (candidate), 4. Author-only-match (fallback criteria), 5. Other (vaguely matching genre or unlikely keywords)] " +
            "For each hypothesis, provide at least one of: title, author, or keywords (1-5 relevant search terms). " +
            "Additionally provide: confidence (1-5 integer, based on which criteria number was matched), and reasoning (1-2 sentences explaining why this book matches). " +
            "If the best hypothesis for the match relies on the author fallback criteria (exact or near author-only-match), default to up to 5 distinct hypotheses for top works by that author. " +
            "If the best hypothesis for the match relies only on the weakest criteria (other vague connections), default to up to 5 distinct hypotheses with keywords only (no title or author fields). " +
            "If there is more than one author, mention which author(s) is primary and which author(s) is not in the reasoning. " +
            "List the hypotheses in order of ascending confidence integer. " +
            "Limit your hypotheses to canonical works by their authors. " +
            "Do not provide duplicate hypotheses for the same book. " +
            "Example response format: {\"hypotheses\": [{\"title\": \"The Hobbit\", \"author\": \"J R R Tolkien\", \"keywords\": [\"hobbit\", \"fantasy\"], \"confidence\": 1, \"reasoning\": \"Exact title and author match from user query.\"}]}");
        chatHistory.AddUserMessage(blob);

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel);

        var result = JsonSerializer.Deserialize<LlmBookHypothesisResponseSchema>(response.Content!);
        return result ?? new LlmBookHypothesisResponseSchema { Hypotheses = [] };
    }

    // LLM matches the best candidate work from each list of candidate works to each corresponding LLM hypothesis
    // LLM orders the matched works by strength of match to original user query
    public async Task<LlmRankedMatchResponseSchema> RankCandidatesAsync(
        LlmRankAndMatchCandidatesRequest request,
        LlmModel? model = null,
        float? temperature = null,
        int? maxTokens = null)
    {
        var (chatCompletionService, settings) = CreateConfiguredChatCompletionService(
            model,
            temperature,
            maxTokens,
            typeof(LlmRankedMatchResponseSchema));

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            "You are a book matching expert. " +
            "Given a user's original query and a list of LLM-generated hypotheses grouped with candidate works from OpenLibrary's API, select the single best matching work_key for each hypothesis. " +
            "Then, rank the selected works in descending order of strongest match to the original user query. " +
            "For each selected work, identify primary authors (typically the main writers) and contributors (illustrators, editors, adaptors, etc.) from the author list. " +
            "Provide a 1-2 sentence explanation citing: specific matching criteria (exact title, author match, etc.), author roles, and reasoning for the ranking. " +
            "De-duplicate by work_key - if the same work appears multiple times, include it only once. " +
            "Example response format: {\"matches\": [{\"work_key\": \"/works/OL45883W\", \"primary_authors\": [\"J.R.R. Tolkien\"], \"contributors\": [\"Charles Dixon\"], \"explanation\": \"Exact title and author match for 'The Hobbit'; Tolkien is primary author, Dixon is adaptor. Ranked first due to perfect match.\"}]}");

        var requestJson = JsonSerializer.Serialize(request);
        chatHistory.AddUserMessage(requestJson);

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel);

        var result = JsonSerializer.Deserialize<LlmRankedMatchResponseSchema>(response.Content!);
        return result ?? new LlmRankedMatchResponseSchema { Matches = [] };
    }
}

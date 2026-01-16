using BookMatcher.Common.Models.Configurations;
using BookMatcher.Services;
using BookMatcher.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// required to use experimental SK connectors (Google)
#pragma warning disable SKEXP0070

var builder = WebApplication.CreateBuilder(args);

// add services and controllers to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// configure settings from appsettings.json
builder.Services.Configure<GeminiConfiguration>(
    builder.Configuration.GetSection("GeminiConfiguration"));
builder.Services.Configure<OpenAiConfiguration>(
    builder.Configuration.GetSection("OpenAiConfiguration"));
builder.Services.Configure<DefaultLlmSettings>(
    builder.Configuration.GetSection("DefaultLlmSettings"));
builder.Services.Configure<OpenLibraryConfiguration>(
    builder.Configuration.GetSection("OpenLibraryConfiguration"));

// configure Semantic Kernel with multiple LLM models
builder.Services.AddSingleton<Kernel>(provider =>
{
    var geminiConfig = provider.GetRequiredService<IOptions<GeminiConfiguration>>().Value;
    var openAiConfig = provider.GetRequiredService<IOptions<OpenAiConfiguration>>().Value;

    // register LLM models to kernel
    var kernelBuilder = Kernel.CreateBuilder();

    // (default model)
    kernelBuilder.AddGoogleAIGeminiChatCompletion(
        modelId: geminiConfig.FlashLiteModel,
        apiKey: geminiConfig.ApiKey,
        serviceId: "gemini-flash-lite");

    kernelBuilder.AddGoogleAIGeminiChatCompletion(
        modelId: geminiConfig.FlashModel,
        apiKey: geminiConfig.ApiKey,
        serviceId: "gemini-flash");

    kernelBuilder.AddOpenAIChatCompletion(
        modelId: openAiConfig.NanoModel,
        apiKey: openAiConfig.ApiKey,
        serviceId: "gpt-nano");

    return kernelBuilder.Build();
});

// register OpenLibraryService HttpClient with retry policy
builder.Services.AddHttpClient(nameof(OpenLibraryService), (provider, client) =>
{
    var config = provider.GetRequiredService<IOptions<OpenLibraryConfiguration>>().Value;
    client.BaseAddress = new Uri(config.ApiBaseUrl);
}).AddStandardResilienceHandler(options =>
{
    // configure retry policy
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
});

// register application services
builder.Services.AddScoped<IOpenLibraryService, OpenLibraryService>();
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<IBookMatchService, BookMatchService>();

// configure OpenTelemetry for tracing and metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("BookMatcher.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation(options =>
        {
            // enrich traces with request and response content
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.request.body", request.Content?.ReadAsStringAsync().Result);
            };
            options.EnrichWithHttpResponseMessage = (activity, response) =>
            {
                activity.SetTag("http.response.body", response.Content?.ReadAsStringAsync().Result);
            };
        })
        .AddSource("Microsoft.SemanticKernel*")
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Microsoft.SemanticKernel*")
        .AddConsoleExporter());

// add and configure swagger endpoint documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
});

var app = builder.Build();

// configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// register controller routes to HTTP request pipeline
app.MapControllers();
app.UseHttpsRedirection();

app.Run();
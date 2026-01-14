using BookMatcher.Common.Models.Configurations;
using BookMatcher.Services;
using BookMatcher.Services.Interfaces;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// add services and controllers to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// configure OpenLibrary settings from appsettings.json
builder.Services.Configure<OpenLibraryConfiguration>(
    builder.Configuration.GetSection("OpenLibraryConfiguration"));

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
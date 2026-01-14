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

// register OpenLibraryService HttpClient
builder.Services.AddHttpClient(nameof(OpenLibraryService), (provider, client) =>
{
    var config = provider.GetRequiredService<IOptions<OpenLibraryConfiguration>>().Value;
    client.BaseAddress = new Uri(config.ApiBaseUrl);
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
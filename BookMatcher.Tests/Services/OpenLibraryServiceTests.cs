using System.Net;
using BookMatcher.Common.Models.Configurations;
using BookMatcher.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace BookMatcher.Tests.Services;

public class OpenLibraryServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IOptions<OpenLibraryConfiguration>> _configMock = new();
    private readonly OpenLibraryService _openLibraryService;

    public OpenLibraryServiceTests()
    {
        var config = new OpenLibraryConfiguration
        {
            ApiBaseUrl = "https://openlibrary.org"
        };
        _configMock.Setup(c => c.Value).Returns(config);

        _openLibraryService = new OpenLibraryService(_httpClientFactoryMock.Object, _configMock.Object);
    }

    // tests the deserialization of JSON into a valid OpenLibrarySearchResponse
    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsSearchResponse()
    {
        // arrange
        var mockHandler = TestHelpers.CreateMockHttpMessageHandler(
            HttpStatusCode.OK,
            new
            {
                start = 0,
                numFound = 1,
                docs = new[]
                {
                    new
                    {
                        key = "/works/OL53908W",
                        title = "The Adventures of Huckleberry Finn",
                        author_name = new[] { "Mark Twain" },
                        author_key = new[] { "OL18319A" },
                        first_publish_year = 1884,
                        cover_i = 12345,
                        edition_count = 150
                    }
                }
            });

        _httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(mockHandler.Object) { BaseAddress = new Uri(_configMock.Object.Value.ApiBaseUrl) })
            .Verifiable();

        // act
        var result = await _openLibraryService.SearchAsync(query: "huckleberry finn");

        // assert
        Assert.NotNull(result);
        Assert.Equal(1, result.NumFound);
        Assert.Single(result.Docs);
        Assert.Equal("The Adventures of Huckleberry Finn", result.Docs[0].Title);
        Assert.Equal("Mark Twain", result.Docs[0].AuthorName?[0]);
        Assert.Equal(12345, result.Docs[0].CoverId);
        Assert.Equal(150, result.Docs[0].EditionCount);

        _httpClientFactoryMock.Verify();
        // verify
        mockHandler.Verify();
        mockHandler.VerifyNoOtherCalls();
    }
}
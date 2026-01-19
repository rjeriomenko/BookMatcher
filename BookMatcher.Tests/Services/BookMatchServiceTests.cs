using BookMatcher.Common.Models.Responses.Llm;
using BookMatcher.Common.Models.Responses.OpenLibrary;
using BookMatcher.Services;
using BookMatcher.Services.Interfaces;
using Moq;

namespace BookMatcher.Tests.Services;

public class BookMatchServiceTests
{
    // tests the mapping of valid LlmBookHypothesisResponseSchema and LlmRankedMatchResponseSchema classes to List<BookMatch>
    [Fact]
    public async Task FindBookMatchesAsync_WithValidQuery_ReturnsMatches()
    {
        // arrange
        var mockLlmService = new Mock<ILlmService>();
        var mockOpenLibraryService = new Mock<IOpenLibraryService>();

        // mock LLM hypothesis generation
        var hypothesesResponse = new LlmBookHypothesisResponseSchema
        {
            Hypotheses =
            [
                new LlmBookHypothesis
                {
                    Title = "the hobbit",
                    Author = "j r r tolkien",
                    Keywords = ["hobbit", "fantasy"],
                    Confidence = 1,
                    Reasoning = "Exact match"
                }
            ]
        };

        mockLlmService.Setup(l => l.ExtractLlmBookMatchHypothesesAsync(
                It.IsAny<string>(),
                It.IsAny<Common.Enums.LlmModel?>(),
                It.IsAny<float?>(),
                It.IsAny<int?>()))
            .ReturnsAsync(hypothesesResponse);

        // mock OpenLibrary search
        var searchResponse = new OpenLibraryWorkSearchResponse
        {
            NumFound = 1,
            Docs =
            [
                new OpenLibraryWorkDocument
                {
                    Key = "/works/OL262758W",
                    Title = "The Hobbit",
                    AuthorName = ["J.R.R. Tolkien"],
                    FirstPublishYear = 1937,
                    CoverId = 12345,
                    EditionCount = 100
                }
            ]
        };

        mockOpenLibraryService.Setup(o => o.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ReturnsAsync(searchResponse);

        // mock edition key fetch
        mockOpenLibraryService.Setup(o => o.GetFirstEditionKeyAsync(It.IsAny<string>()))
            .ReturnsAsync("/books/OL123456M");

        // mock LLM ranking
        var rankedResponse = new LlmRankedMatchResponseSchema
        {
            Matches =
            [
                new LlmRankedMatch
                {
                    WorkKey = "/works/OL262758W",
                    PrimaryAuthors = ["J.R.R. Tolkien"],
                    Contributors = [],
                    Reasoning = "Exact title and author match"
                }
            ]
        };

        mockLlmService.Setup(l => l.RankCandidatesAsync(
                It.IsAny<Common.Models.Requests.Llm.LlmRankAndMatchCandidatesRequest>(),
                It.IsAny<Common.Enums.LlmModel?>(),
                It.IsAny<float?>(),
                It.IsAny<int?>()))
            .ReturnsAsync(rankedResponse);

        var service = new BookMatchService(
            mockLlmService.Object,
            mockOpenLibraryService.Object);

        // act
        var result = await service.FindBookMatchesAsync("there and back again");

        // assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("The Hobbit", result.First().Title);
        Assert.Contains("J.R.R. Tolkien", result.First().PrimaryAuthors);
        Assert.Equal("/works/OL262758W", result.First().WorkKey);
        Assert.Contains("/books/OL123456M", result.First().OpenLibraryUrl);
        Assert.Contains("/b/id/12345-L.jpg", result.First().CoverUrl);
        Assert.Equal(1937, result.First().FirstPublishYear);

        // verify services were called
        mockLlmService.Verify(l => l.ExtractLlmBookMatchHypothesesAsync(
            It.IsAny<string>(),
            It.IsAny<Common.Enums.LlmModel?>(),
            It.IsAny<float?>(),
            It.IsAny<int?>()), Times.Once);

        mockLlmService.Verify(l => l.RankCandidatesAsync(
            It.IsAny<Common.Models.Requests.Llm.LlmRankAndMatchCandidatesRequest>(),
            It.IsAny<Common.Enums.LlmModel?>(),
            It.IsAny<float?>(),
            It.IsAny<int?>()), Times.Once);
    }
}

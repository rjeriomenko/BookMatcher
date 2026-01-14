using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;

namespace BookMatcher.Tests;

public class TestHelpers
{
    // creates a mock HttpMessageHandler that returns the specified status code and response body
    // used to mock HttpClient behavior in unit tests without making real HTTP requests
    public static Mock<HttpMessageHandler> CreateMockHttpMessageHandler(
        HttpStatusCode returnCode, object? responseBodyObject = null)
    {
        var mock = new Mock<HttpMessageHandler>();

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = returnCode,
                Content = responseBodyObject != null ? JsonContent.Create(responseBodyObject) : null
            })
            .Verifiable();

        return mock;
    }
}
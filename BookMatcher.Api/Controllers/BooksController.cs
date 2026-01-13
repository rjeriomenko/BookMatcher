using System.Net.Mime;
using BookMatcher.Common.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BookMatcher.Api.Controllers;

[ApiController]
[Route("api/books")]
// explicitly define response type for swagger documentation
[Produces(MediaTypeNames.Application.Json)]
public class BooksController : ControllerBase
{
    public BooksController()
    {
        // private fields go in the constructor for dependency injection
    }

    // explicitly define http response codes and response schema for swagger documentation
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<BookMatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Search([FromQuery] string query)
    {
        try
        {
            // hardcoded response for testing
            var results = new List<BookMatchResponse>
            {
                new BookMatchResponse
                {
                    Title = query,
                    Author = "J.R.R. Tolkien",
                    FirstPublishYear = 1937,
                    OpenLibraryId = "12345",
                    OpenLibraryUrl = "https://bookurl.com",
                    CoverImageUrl = "https://coverurl.jpg",
                    Explanation = "Exact title match; Tolkien is primary author; Dixon listed as adaptor."
                }
            };
            
            return Ok(results);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
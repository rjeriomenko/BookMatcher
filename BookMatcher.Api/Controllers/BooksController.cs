using System.Net.Mime;
using BookMatcher.Common.Models.Responses;
using BookMatcher.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookMatcher.Api.Controllers;

[ApiController]
[Route("api/books")]
// explicitly define response type for swagger documentation
[Produces(MediaTypeNames.Application.Json)]
public class BooksController : ControllerBase
{
    private readonly IOpenLibraryService _openLibraryService;
    
    public BooksController(IOpenLibraryService openLibraryService)
    {
        // inject services via dependency injection
        _openLibraryService = openLibraryService;
    }

    // explicitly define http response codes and response schema for swagger documentation
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<BookMatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        try
        {
            var results = await _openLibraryService.SearchAsync(query);
            return results is not null ? Ok(results) : throw new Exception();
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
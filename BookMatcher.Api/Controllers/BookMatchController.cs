using System.Net.Mime;
using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Responses;
using BookMatcher.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookMatcher.Api.Controllers;

[ApiController]
[Route("api/bookMatch")]
// explicitly define response type for swagger documentation
[Produces(MediaTypeNames.Application.Json)]
public class BookMatchController : ControllerBase
{
    private readonly IBookMatchService _bookMatchService;

    public BookMatchController(IBookMatchService bookMatchService)
    {
        // inject services via dependency injection
        _bookMatchService = bookMatchService;
    }

    // find best matching books for user's messy query using LLM hypotheses, multi-stage OpenLibrary API search, and LLM ranking
    // explicitly define http response codes and response schema for swagger documentation
    [HttpGet("match")]
    [ProducesResponseType(typeof(BookMatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Match([FromQuery] string query, [FromQuery] LlmModel? model = null)
    {
        try
        {
            var results = await _bookMatchService.FindBookMatchesAsync(query, model);
            return Ok(new BookMatchResponse { Matches = results });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
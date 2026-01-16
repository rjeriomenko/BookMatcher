using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using BookMatcher.Common.Enums;
using BookMatcher.Common.Exceptions;
using BookMatcher.Common.Models.Responses;
using BookMatcher.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Match(
        [FromQuery] string query,
        [FromQuery] LlmModel? model = null,
        [FromQuery] [Range(0.0, 1.0)] float? temperature = null)
    {
        try
        {
            var results = await _bookMatchService.FindBookMatchesAsync(query, model, temperature);

            // return 404 if no matches found
            if (results.Count == 0)
            {
                return NotFound(new { message = "No book matches found for the given query" });
            }

            return Ok(new BookMatchResponse { Matches = results });
        }
        catch (LlmServiceException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "LLM service unavailable", error = ex.Message });
        }
        catch (OpenLibraryServiceException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "OpenLibrary service unavailable", error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred", error = ex.Message });
        }
    }
}
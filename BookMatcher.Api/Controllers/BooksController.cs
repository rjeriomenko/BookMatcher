using System.Net.Mime;
using BookMatcher.Common.Enums;
using BookMatcher.Common.Models.Responses.OpenLibrary;
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
    private readonly ILlmService _llmService;

    public BooksController(IOpenLibraryService openLibraryService, ILlmService llmService)
    {
        // inject services via dependency injection
        _openLibraryService = openLibraryService;
        _llmService = llmService;
    }

    // extract book hypotheses from messy user query using LLM, then search OpenLibrary for each
    // explicitly define http response codes and response schema for swagger documentation
    [HttpGet("match")]
    [ProducesResponseType(typeof(List<OpenLibraryWorkDocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Match([FromQuery] string query, [FromQuery] LlmModel? model = null)
    {
        try
        {
            // extract hypotheses from blob using LLM
            var hypothesesResponse = await _llmService.ExtractHypothesesAsync(query, model);

            // search OpenLibrary for each hypothesis
            var allDocs = new List<OpenLibraryWorkDocumentResponse>();
            foreach (var hypothesis in hypothesesResponse.Hypotheses)
            {
                var keywordsQuery = hypothesis.Keywords is { Count: > 0 }
                    ? string.Join(" ", hypothesis.Keywords)
                    : null;

                var response = await _openLibraryService.SearchAsync(
                    query: keywordsQuery,
                    title: hypothesis.Title,
                    author: hypothesis.Author,
                    limit: 5);

                if (response?.Docs != null)
                {
                    allDocs.AddRange(response.Docs);
                }
            }

            return Ok(allDocs);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
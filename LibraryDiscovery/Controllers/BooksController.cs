using LibraryDiscovery.Application.DTOs;
using LibraryDiscovery.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryDiscovery.Controllers;

/// <summary>
/// API endpoint for book matching.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookMatchService _bookMatchService;
    private readonly ILogger<BooksController> _logger;

    public BooksController(IBookMatchService bookMatchService, ILogger<BooksController> logger)
    {
        _bookMatchService = bookMatchService ?? throw new ArgumentNullException(nameof(bookMatchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Matches a query to books in Open Library.
    /// </summary>
    /// <param name="request">Book match request with query string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matched books with explanations.</returns>
    [HttpPost("match")]
    public async Task<ActionResult<BookMatchResponse>> MatchBooks(
        [FromBody] BookMatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query cannot be empty" });
        }

        _logger.LogInformation("Book match request: {Query}", request.Query);

        var response = await _bookMatchService.MatchAsync(request.Query, cancellationToken);

        if (response.Matches.Count == 0 && string.IsNullOrEmpty(response.Message))
        {
            _logger.LogInformation("No matches found for query: {Query}", request.Query);
            return Ok(response); // Return 200 with empty matches
        }

        _logger.LogInformation("Found {Count} matches for query: {Query}", response.Matches.Count, request.Query);
        return Ok(response);
    }
}

/// <summary>
/// Request model for book matching endpoint.
/// </summary>
public class BookMatchRequest
{
    /// <summary>
    /// The user's book search query (e.g., "Tolkien Hobbit 1937").
    /// </summary>
    public string Query { get; set; } = string.Empty;
}

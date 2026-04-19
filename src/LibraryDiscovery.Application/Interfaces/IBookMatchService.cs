using LibraryDiscovery.Application.DTOs;

namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Main orchestrator for book matching workflow.
/// Coordinates query parsing, search, enrichment, ranking, and explanation generation.
/// </summary>
public interface IBookMatchService
{
    /// <summary>
    /// Matches a messy user query to candidate books.
    /// </summary>
    /// <param name="rawQuery">The messy plain-text query (e.g., "tolkien hobbit illustrated 1937").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered list of matched books with explanations.</returns>
    Task<BookMatchResponse> MatchAsync(string rawQuery, CancellationToken cancellationToken);
}

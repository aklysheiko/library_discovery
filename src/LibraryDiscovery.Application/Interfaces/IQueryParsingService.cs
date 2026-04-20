namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Parses messy text queries using LLM with a fallback parser.
/// </summary>
public interface IQueryParsingService
{
    /// <summary>
    /// Parses a messy query into structured title, author, and keyword candidates.
    /// Tries LLM first; falls back to regex/heuristic parser on failure.
    /// </summary>
    /// <param name="rawQuery">The raw messy query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Structured parsed query with title/author/keyword candidates.</returns>
    Task<ParsedQuery> ParseAsync(string rawQuery, CancellationToken cancellationToken);
}

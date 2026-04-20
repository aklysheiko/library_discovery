namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Enriches search results with additional data (work details, author info, etc.).
/// Converts Open Library search docs to normalized BookCandidate entities.
/// </summary>
public interface ICandidateEnrichmentService
{
    /// <summary>
    /// Enriches search results by fetching work details, resolving primary authors, and normalizing data.
    /// </summary>
    /// <param name="docs">Raw search results from Open Library.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of enriched book candidates ready for matching.</returns>
    Task<IReadOnlyList<BookCandidate>> EnrichAsync(
        IReadOnlyList<OpenLibrarySearchDoc> docs,
        CancellationToken cancellationToken);
}

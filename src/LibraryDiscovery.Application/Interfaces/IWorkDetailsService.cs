namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Fetches canonical work and author data from Open Library detail endpoints.
/// Used to resolve true primary authors from works.authors, as the search API
/// mixes contributors (illustrators, editors) into the author_name field.
/// </summary>
public interface IWorkDetailsService
{
    /// <summary>
    /// Returns the primary author names for a work by fetching
    /// /works/{workId}.json and resolving each /authors/{authorId}.json.
    /// Returns an empty array on any failure (best-effort).
    /// </summary>
    Task<string[]> GetPrimaryAuthorsAsync(string workId, CancellationToken cancellationToken);
}

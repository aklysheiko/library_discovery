namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Search request for Open Library API.
/// </summary>
public class OpenLibrarySearchRequest
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? RawQuery { get; set; }
    public int Limit { get; set; } = 10;
}

/// <summary>
/// Represents a search result document from Open Library.
/// </summary>
public class OpenLibrarySearchDoc
{
    public string Key { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    public string? Author_name { get; set; }
    public string[]? Author_names { get; set; }
    
    public int? First_publish_year { get; set; }
    public int? Edition_count { get; set; }
    
    public string? Cover_id { get; set; }
    
    /// <summary>
    /// Raw JSON from Open Library for potential re-enrichment.
    /// </summary>
    public Dictionary<string, object>? RawData { get; set; }
}

/// <summary>
/// Searches Open Library for book candidates.
/// </summary>
public interface IOpenLibrarySearchService
{
    /// <summary>
    /// Searches Open Library with given criteria.
    /// </summary>
    /// <param name="request">Search parameters (title, author, raw query).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results from Open Library API.</returns>
    Task<IReadOnlyList<OpenLibrarySearchDoc>> SearchAsync(
        OpenLibrarySearchRequest request,
        CancellationToken cancellationToken);
}

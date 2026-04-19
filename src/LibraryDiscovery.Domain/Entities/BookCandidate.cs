namespace LibraryDiscovery.Domain.Entities;

/// <summary>
/// Represents a book candidate found during search and enrichment.
/// This is the normalized, enriched version ready for matching.
/// </summary>
public class BookCandidate
{
    public string OpenLibraryWorkId { get; set; } = string.Empty;
    public string? OpenLibraryKey { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? NormalizedTitle { get; set; }
    
    public string[] PrimaryAuthors { get; set; } = Array.Empty<string>();
    public string[] ContributorAuthors { get; set; } = Array.Empty<string>();
    public string? NormalizedPrimaryAuthorSurnames { get; set; }
    
    public int FirstPublishYear { get; set; }
    
    public string? CoverUrl { get; set; }
    
    /// <summary>
    /// Number of editions available for this work (proxy for popularity/canonical status).
    /// </summary>
    public int EditionCount { get; set; }
    
    /// <summary>
    /// Raw data from Open Library for debugging/explanation.
    /// </summary>
    public Dictionary<string, object> RawData { get; set; } = new();
}

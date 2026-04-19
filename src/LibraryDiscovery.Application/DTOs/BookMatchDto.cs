namespace LibraryDiscovery.Application.DTOs;

/// <summary>
/// A single matched book result returned to the client.
/// </summary>
public class BookMatchResultDto
{
    public string Title { get; set; } = string.Empty;
    
    public string[] PrimaryAuthors { get; set; } = Array.Empty<string>();
    
    public int FirstPublishYear { get; set; }
    
    public string? OpenLibraryWorkId { get; set; }
    
    public string? CoverUrl { get; set; }
    
    /// <summary>
    /// Matching score (0-100).
    /// </summary>
    public int Score { get; set; }
    
    /// <summary>
    /// Short grounded explanation of why this book matched.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// Response returned from the book match endpoint.
/// </summary>
public class BookMatchResponse
{
    /// <summary>
    /// The original user query.
    /// </summary>
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// The parsed query structure for client reference.
    /// </summary>
    public ParsedQueryDto? ParsedQuery { get; set; }
    
    /// <summary>
    /// Ordered list of matched books (best match first).
    /// </summary>
    public IReadOnlyList<BookMatchResultDto> Matches { get; set; } = Array.Empty<BookMatchResultDto>();
    
    /// <summary>
    /// Optional error or warning message.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// DTO for the parsed query structure.
/// </summary>
public class ParsedQueryDto
{
    public string[] TitleCandidates { get; set; } = Array.Empty<string>();
    public string[] AuthorCandidates { get; set; } = Array.Empty<string>();
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public int? YearHint { get; set; }
}

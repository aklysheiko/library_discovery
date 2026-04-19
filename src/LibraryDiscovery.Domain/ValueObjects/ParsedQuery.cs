namespace LibraryDiscovery.Domain.ValueObjects;

/// <summary>
/// Represents a parsed and normalized query extracted by the LLM or fallback parser.
/// </summary>
public class ParsedQuery
{
    public string RawQuery { get; set; } = string.Empty;
    
    /// <summary>
    /// Possible book titles extracted from the query.
    /// </summary>
    public IReadOnlyList<string> TitleCandidates { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Possible author names extracted from the query.
    /// </summary>
    public IReadOnlyList<string> AuthorCandidates { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Keywords or edition hints (e.g., "illustrated", "deluxe").
    /// </summary>
    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Year hint if detected (e.g., from "1937").
    /// </summary>
    public int? YearHint { get; set; }
    
    /// <summary>
    /// Parser confidence notes for debugging.
    /// </summary>
    public string? ConfidenceNotes { get; set; }
}

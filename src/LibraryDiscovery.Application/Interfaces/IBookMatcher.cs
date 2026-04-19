using LibraryDiscovery.Domain.Entities;
using LibraryDiscovery.Domain.ValueObjects;

namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Represents the evaluation of a match between a parsed query and a candidate book.
/// </summary>
public class MatchEvaluation
{
    /// <summary>
    /// The candidate book being evaluated.
    /// </summary>
    public BookCandidate Candidate { get; set; } = null!;
    
    /// <summary>
    /// The matching strength (100 = exact, 0 = no match).
    /// </summary>
    public int Score { get; set; }
    
    /// <summary>
    /// Ordered list of reasons this match scored well (e.g., "exact title", "primary author match").
    /// Used for explanation generation.
    /// </summary>
    public IReadOnlyList<string> MatchReasons { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Applies deterministic ranking hierarchy to candidates based on parsed query.
/// </summary>
public interface IBookMatcher
{
    /// <summary>
    /// Ranks candidates using deterministic scoring rules.
    /// Returns all candidates sorted by score (descending).
    /// </summary>
    /// <param name="parsedQuery">Parsed query with title/author/keyword candidates.</param>
    /// <param name="candidates">Enriched book candidates to rank.</param>
    /// <returns>Candidates ranked by match strength, with match evaluations.</returns>
    IReadOnlyList<MatchEvaluation> RankCandidates(
        ParsedQuery parsedQuery,
        IReadOnlyList<BookCandidate> candidates);
}

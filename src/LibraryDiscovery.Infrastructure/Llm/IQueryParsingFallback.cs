using LibraryDiscovery.Domain.ValueObjects;

namespace LibraryDiscovery.Infrastructure.Llm;

/// <summary>
/// Fallback parser interface for regex/heuristic-based query parsing.
/// </summary>
public interface IQueryParsingFallback
{
    /// <summary>
    /// Parses query using heuristics when LLM is unavailable.
    /// </summary>
    ParsedQuery Parse(string rawQuery);
}

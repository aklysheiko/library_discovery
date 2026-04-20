using System.Text.RegularExpressions;

namespace LibraryDiscovery.Infrastructure.Llm;

/// <summary>
/// Regex/heuristic-based query parser for when Gemini API is unavailable.
/// Handles common patterns: "book_title by author", "author's book", etc.
/// </summary>
public class FallbackQueryParser : IQueryParsingFallback
{
    private static readonly string[] AuthorIndicators = { " by ", " author ", " from " };
    private static readonly string[] YearPatterns = { @"\b((?:19|20)\d{2})\b", @"\(([12]\d{3})\)" };
    private static readonly string[] KeywordPatterns = { "illustrated", "deluxe", "hardcover", "paperback", "edition", "anniversary" };

    /// <summary>
    /// Parses query using regex and heuristics.
    /// </summary>
    public ParsedQuery Parse(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
            return new ParsedQuery { RawQuery = rawQuery, ConfidenceNotes = "Empty input" };

        var titleCandidates = new List<string>();
        var authorCandidates = new List<string>();
        var keywords = new List<string>();
        int? yearHint = null;

        var workingQuery = rawQuery.ToLower();

        // Extract year hints
        yearHint = ExtractYear(workingQuery);

        // Extract keywords
        ExtractKeywords(workingQuery, keywords);

        // Split by author indicators (e.g., "book by someone")
        var (title, author) = SplitByAuthorIndicators(rawQuery);

        if (!string.IsNullOrWhiteSpace(title))
            titleCandidates.Add(title.Trim());

        if (!string.IsNullOrWhiteSpace(author))
            authorCandidates.Add(author.Trim());

        // Try additional extraction: split by quotes
        ExtractQuotedPhrases(rawQuery, titleCandidates);

        // Confidence assessment
        var confidence = titleCandidates.Count > 0 ? "medium" : "low";
        var notes = $"Fallback parser extracted {titleCandidates.Count} titles, {authorCandidates.Count} authors";

        return new ParsedQuery
        {
            RawQuery = rawQuery,
            TitleCandidates = titleCandidates.Distinct().ToList().AsReadOnly(),
            AuthorCandidates = authorCandidates.Distinct().ToList().AsReadOnly(),
            Keywords = keywords.Distinct().ToList().AsReadOnly(),
            YearHint = yearHint,
            ConfidenceNotes = notes
        };
    }

    /// <summary>
    /// Splits query on " by " and similar author indicators.
    /// Returns (title, author) tuple.
    /// </summary>
    private (string title, string author) SplitByAuthorIndicators(string query)
    {
        foreach (var indicator in AuthorIndicators)
        {
            if (query.Contains(indicator, StringComparison.OrdinalIgnoreCase))
            {
                var parts = Regex.Split(query, Regex.Escape(indicator), RegexOptions.IgnoreCase);
                if (parts.Length == 2)
                {
                    return (parts[0].Trim(), parts[1].Trim());
                }
            }
        }

        // No explicit author indicator found - apply heuristic to detect author-name queries.
        // Heuristic: short queries (2-3 tokens) where most tokens start with an uppercase letter
        // are likely author names (e.g., "Agatha Christie", "J R R Tolkien").
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim(new char[] {',', '.', '"', '\'', ':'}))
                          .Where(t => t.Length > 0)
                          .ToArray();

        if (tokens.Length >= 2 && tokens.Length <= 3)
        {
            var capitalizedCount = tokens.Count(t => char.IsLetter(t[0]) && char.IsUpper(t[0]));
            if (capitalizedCount >= tokens.Length)
            {
                // Treat as author-only query
                return (string.Empty, query.Trim());
            }
        }

        // No indicator and not detected as author -> treat entire query as potential title
        return (query, string.Empty);
    }

    /// <summary>
    /// Extracts 4-digit year from query.
    /// </summary>
    private int? ExtractYear(string query)
    {
        foreach (var pattern in YearPatterns)
        {
            var match = Regex.Match(query, pattern);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var year))
            {
                // Validate it's a reasonable publication year
                if (year >= 1450 && year <= DateTime.Now.Year)
                    return year;
            }
        }
        return null;
    }

    /// <summary>
    /// Extracts known edition keywords (e.g., "illustrated", "deluxe").
    /// </summary>
    private void ExtractKeywords(string query, List<string> keywords)
    {
        var lowerQuery = query.ToLower();
        foreach (var keyword in KeywordPatterns)
        {
            if (lowerQuery.Contains(keyword))
                keywords.Add(keyword);
        }
    }

    /// <summary>
    /// Extracts text in quotes as potential titles.
    /// </summary>
    private void ExtractQuotedPhrases(string query, List<string> titleCandidates)
    {
        // Match text within double or single quotes
        var matches = Regex.Matches(query, @"[""']([^""']+)[""']");
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var quoted = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(quoted) && !titleCandidates.Contains(quoted))
                    titleCandidates.Add(quoted);
            }
        }
    }
}

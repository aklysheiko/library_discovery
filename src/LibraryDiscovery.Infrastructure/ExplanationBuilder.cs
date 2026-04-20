namespace LibraryDiscovery.Infrastructure;

/// <summary>
/// Generates short, grounded explanations for why a book matched the query.
/// </summary>
public class ExplanationBuilder : IExplanationBuilder
{
    /// <summary>
    /// Builds a 1-2 sentence explanation grounded in match reasons and candidate data.
    /// Author-only queries get a phrasing that highlights the author relationship.
    /// </summary>
    public string Build(MatchEvaluation evaluation)
    {
        if (evaluation.MatchReasons.Count == 0)
            return "Book matched the search query.";

        var topReasons = evaluation.MatchReasons.Take(2).ToList();

        // Author-only mode: match reasons start with author-related phrases.
        // Prepend a short label so the explanation reads naturally.
        var firstReason = topReasons[0];
        bool isAuthorCentric =
            firstReason.StartsWith("Exact primary author", StringComparison.OrdinalIgnoreCase) ||
            firstReason.StartsWith("Primary author surname", StringComparison.OrdinalIgnoreCase);

        if (isAuthorCentric && evaluation.Candidate.PrimaryAuthors.Length > 0)
        {
            var author = evaluation.Candidate.PrimaryAuthors[0];
            return topReasons.Count > 1
                ? $"Top work by {author}. {topReasons[1]}."
                : $"Top work by {author}.";
        }

        return string.Join(" ", topReasons);
    }
}


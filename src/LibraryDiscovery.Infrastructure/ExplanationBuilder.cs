namespace LibraryDiscovery.Infrastructure;

/// <summary>
/// Generates short, grounded explanations for why a book matched the query.
/// </summary>
public class ExplanationBuilder : IExplanationBuilder
{
    /// <summary>
    /// Builds a 1-2 sentence explanation grounded in match reasons and candidate data.
    /// </summary>
    public string Build(MatchEvaluation evaluation)
    {
        if (evaluation.MatchReasons.Count == 0)
            return "Book matched the search query.";

        // Take the top 2 reasons
        var topReasons = evaluation.MatchReasons.Take(2).ToList();
        
        var reasonText = string.Join(" ", topReasons);

        return $"{reasonText}";
    }
}

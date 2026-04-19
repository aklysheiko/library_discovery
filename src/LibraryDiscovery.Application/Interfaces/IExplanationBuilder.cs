namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Generates short, grounded explanations for why a book matched the query.
/// </summary>
public interface IExplanationBuilder
{
    /// <summary>
    /// Builds a 1-2 sentence explanation grounded in match reasons and candidate data.
    /// </summary>
    /// <param name="evaluation">Match evaluation with reasons and candidate data.</param>
    /// <returns>Short explanation string.</returns>
    string Build(MatchEvaluation evaluation);
}

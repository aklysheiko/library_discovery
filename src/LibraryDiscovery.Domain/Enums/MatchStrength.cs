namespace LibraryDiscovery.Domain.Enums;

/// <summary>
/// Ranking strength for book match results.
/// Higher values indicate stronger matches.
/// </summary>
public enum MatchStrength
{
    ExactTitleAndPrimaryAuthor = 100,
    ExactTitleAndContributorAuthor = 80,
    NearTitleAndAuthor = 70,
    AuthorOnlyFallback = 50,
    WeakCandidate = 20,
    NoMatch = 0
}

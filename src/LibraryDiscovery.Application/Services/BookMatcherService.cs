using LibraryDiscovery.Application.Interfaces;
using LibraryDiscovery.Domain.Entities;
using LibraryDiscovery.Domain.ValueObjects;

namespace LibraryDiscovery.Application.Services;

/// <summary>
/// Implements deterministic book matching with clear, auditable scoring.
/// Does not use LLM for final decisions - all logic is transparent C#.
/// </summary>
public class BookMatcherService : IBookMatcher
{
    private readonly IStringNormalizationService _normalizationService;

    public BookMatcherService(IStringNormalizationService normalizationService)
    {
        _normalizationService = normalizationService ?? 
            throw new ArgumentNullException(nameof(normalizationService));
    }

    /// <summary>
    /// Ranks candidates using deterministic scoring rules.
    /// Returns all candidates sorted by score (descending), with highest-scoring first.
    /// </summary>
    public IReadOnlyList<MatchEvaluation> RankCandidates(
        ParsedQuery parsedQuery,
        IReadOnlyList<BookCandidate> candidates)
    {
        if (parsedQuery == null) throw new ArgumentNullException(nameof(parsedQuery));
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));

        var evaluations = candidates
            .Select(candidate => EvaluateCandidate(parsedQuery, candidate))
            .OrderByDescending(e => e.Score)
            .ToList();

        return evaluations.AsReadOnly();
    }

    /// <summary>
    /// Evaluates a single candidate against the parsed query.
    /// Returns a MatchEvaluation with score and detailed match reasons.
    /// </summary>
    private MatchEvaluation EvaluateCandidate(ParsedQuery query, BookCandidate candidate)
    {
        var reasons = new List<string>();
        int score = 0;

        // Score title matching
        var titleScore = ScoreTitle(query, candidate, reasons);
        score += titleScore;

        // Score author matching
        var authorScore = ScoreAuthor(query, candidate, reasons);
        score += authorScore;

        // Score year hint matching
        var yearScore = ScoreYear(query, candidate, reasons);
        score += yearScore;

        // Apply modifier for edition count (popularity/canonical status)
        var editionScore = ScoreEditionCount(candidate, reasons);
        score += editionScore;

        return new MatchEvaluation
        {
            Candidate = candidate,
            Score = Math.Max(0, score), // Ensure non-negative
            MatchReasons = reasons.AsReadOnly()
        };
    }

    /// <summary>
    /// Scores title matching based on exact normalized match, partial match, and token overlap.
    /// Possible points: 0-65
    /// </summary>
    private int ScoreTitle(ParsedQuery query, BookCandidate candidate, List<string> reasons)
    {
        if (query.TitleCandidates.Count == 0)
            return 0;

        int bestTitleScore = 0;
        string bestTitleReason = string.Empty;

        foreach (var titleCandidate in query.TitleCandidates)
        {
            var normalizedQuery = _normalizationService.Normalize(titleCandidate);
            var normalizedCandidate = candidate.NormalizedTitle ?? 
                _normalizationService.Normalize(candidate.Title);

            // Exact normalized match (strongest)
            if (normalizedQuery == normalizedCandidate)
            {
                if (60 > bestTitleScore)
                {
                    bestTitleScore = 60;
                    bestTitleReason = $"Exact title match: '{candidate.Title}'";
                }
                continue;
            }

            // Title starts with query title (near-match)
            if (normalizedCandidate.StartsWith(normalizedQuery))
            {
                if (45 > bestTitleScore)
                {
                    bestTitleScore = 45;
                    bestTitleReason = $"Title starts with: '{candidate.Title}'";
                }
                continue;
            }

            // Query title is contained in candidate (subtitle handling)
            if (normalizedCandidate.Contains(normalizedQuery))
            {
                if (35 > bestTitleScore)
                {
                    bestTitleScore = 35;
                    bestTitleReason = $"Title contains: '{candidate.Title}'";
                }
                continue;
            }

            // Token overlap analysis
            var queryTokens = _normalizationService.ToTokens(titleCandidate);
            var candidateTokens = _normalizationService.ToTokens(candidate.Title);
            var overlapCount = queryTokens.Intersect(candidateTokens).Count();
            var overlapRatio = queryTokens.Length > 0 
                ? (double)overlapCount / queryTokens.Length 
                : 0;

            if (overlapRatio >= 0.5)
            {
                var tokenScore = (int)(25 * overlapRatio); // Up to 25 points
                if (tokenScore > bestTitleScore)
                {
                    bestTitleScore = tokenScore;
                    bestTitleReason = $"Token overlap ({overlapRatio:P0}): '{candidate.Title}'";
                }
            }
        }

        if (bestTitleScore > 0)
            reasons.Add(bestTitleReason);

        return bestTitleScore;
    }

    /// <summary>
    /// Scores author matching: exact primary author, surname match, contributor, or no match.
    /// Possible points: 0-35
    /// </summary>
    private int ScoreAuthor(ParsedQuery query, BookCandidate candidate, List<string> reasons)
    {
        if (query.AuthorCandidates.Count == 0)
            return 0;

        int bestAuthorScore = 0;
        string bestAuthorReason = string.Empty;

        foreach (var authorCandidate in query.AuthorCandidates)
        {
            var normalizedQueryAuthor = _normalizationService.Normalize(authorCandidate);

            // Check against primary authors (highest priority)
            foreach (var primaryAuthor in candidate.PrimaryAuthors)
            {
                var normalizedPrimary = _normalizationService.Normalize(primaryAuthor);

                // Exact primary author match
                if (normalizedQueryAuthor == normalizedPrimary)
                {
                    if (30 > bestAuthorScore)
                    {
                        bestAuthorScore = 30;
                        bestAuthorReason = $"Exact primary author: {primaryAuthor}";
                    }
                }
                // Surname-only match (e.g., "tolkien" matches "J.R.R. Tolkien")
                else if (IsSurnameMatch(normalizedQueryAuthor, normalizedPrimary))
                {
                    if (20 > bestAuthorScore)
                    {
                        bestAuthorScore = 20;
                        bestAuthorReason = $"Primary author surname match: {primaryAuthor}";
                    }
                }
            }

            // Check against contributor authors (lower priority)
            foreach (var contributorAuthor in candidate.ContributorAuthors)
            {
                var normalizedContributor = _normalizationService.Normalize(contributorAuthor);

                if (normalizedQueryAuthor == normalizedContributor)
                {
                    if (15 > bestAuthorScore)
                    {
                        bestAuthorScore = 15;
                        bestAuthorReason = $"Contributor author match: {contributorAuthor}";
                    }
                }
                else if (IsSurnameMatch(normalizedQueryAuthor, normalizedContributor))
                {
                    if (8 > bestAuthorScore)
                    {
                        bestAuthorScore = 8;
                        bestAuthorReason = $"Contributor author surname match: {contributorAuthor}";
                    }
                }
            }
        }

        if (bestAuthorScore > 0)
            reasons.Add(bestAuthorReason);

        return bestAuthorScore;
    }

    /// <summary>
    /// Checks if normalizedQuery is a surname match within normalizedAuthorFull.
    /// E.g., "tolkien" matches "j r r tolkien" (last significant token).
    /// </summary>
    private bool IsSurnameMatch(string normalizedQuery, string normalizedAuthorFull)
    {
        var tokens = _normalizationService.ToTokens(normalizedAuthorFull);
        if (tokens.Length == 0)
            return false;

        // Check if query matches the last token (common surname pattern)
        return tokens.Last() == normalizedQuery;
    }

    /// <summary>
    /// Scores year hint matching.
    /// Possible points: 0-5
    /// </summary>
    private int ScoreYear(ParsedQuery query, BookCandidate candidate, List<string> reasons)
    {
        if (!query.YearHint.HasValue || candidate.FirstPublishYear == 0)
            return 0;

        // Exact year match
        if (query.YearHint.Value == candidate.FirstPublishYear)
        {
            reasons.Add($"Year matches: {query.YearHint}");
            return 5;
        }

        // Close year match (within 2 years, common for reprints/editions)
        if (Math.Abs(query.YearHint.Value - candidate.FirstPublishYear) <= 2)
        {
            reasons.Add($"Year close: query {query.YearHint}, book {candidate.FirstPublishYear}");
            return 2;
        }

        return 0;
    }

    /// <summary>
    /// Scores edition count for tie-breaking (proxy for canonical/popular work).
    /// Possible points: 0-3
    /// </summary>
    private int ScoreEditionCount(BookCandidate candidate, List<string> reasons)
    {
        if (candidate.EditionCount == 0)
            return 0;

        if (candidate.EditionCount >= 50)
        {
            reasons.Add($"High canonical status ({candidate.EditionCount} editions)");
            return 3;
        }

        if (candidate.EditionCount >= 20)
        {
            reasons.Add($"Well-established work ({candidate.EditionCount} editions)");
            return 2;
        }

        return 1;
    }
}

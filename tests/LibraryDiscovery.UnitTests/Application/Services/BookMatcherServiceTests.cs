using LibraryDiscovery.Application.Services;
using LibraryDiscovery.Application.Interfaces;
using LibraryDiscovery.Domain.Entities;
using LibraryDiscovery.Domain.ValueObjects;
using LibraryDiscovery.Infrastructure.Normalization;
using Xunit;

namespace LibraryDiscovery.UnitTests.Application.Services;

public class BookMatcherServiceTests
{
    private readonly IStringNormalizationService _normalizationService;
    private readonly BookMatcherService _matcher;

    public BookMatcherServiceTests()
    {
        _normalizationService = new StringNormalizationService();
        _matcher = new BookMatcherService(_normalizationService);
    }

    #region Title Scoring Tests

    [Fact]
    public void RankCandidates_ExactTitleMatch_Scores60()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "The Hobbit" }
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            NormalizedTitle = "the hobbit",
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 60, $"Expected score >= 60, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Exact title match")), 
            $"Expected 'Exact title match' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    [Fact]
    public void RankCandidates_TitleStartsWith_Scores45()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "The Hobbit" }
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit: Or There and Back Again",
            NormalizedTitle = "the hobbit or there and back again",
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 45 && result[0].Score < 60, 
            $"Expected score 45-59, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("starts with")), 
            $"Expected 'starts with' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    [Fact]
    public void RankCandidates_TitleContains_Scores35()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "Hobbit" }
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit: Or There and Back Again",
            NormalizedTitle = "the hobbit or there and back again",
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 35 && result[0].Score < 45,
            $"Expected score 35-44, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("contains")), 
            $"Expected 'contains' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    [Fact]
    public void RankCandidates_TokenOverlap_Scores12To25()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "Tolkien Mystery" }
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit: An Unexpected Mystery",
            NormalizedTitle = "the hobbit an unexpected mystery",
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 12 && result[0].Score < 35,
            $"Expected score 12-34, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Token overlap")), 
            $"Expected 'Token overlap' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    #endregion

    #region Author Scoring Tests

    [Fact]
    public void RankCandidates_ExactPrimaryAuthor_Scores30()
    {
        var query = new ParsedQuery
        {
            AuthorCandidates = new[] { "J.R.R. Tolkien" }
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            PrimaryAuthors = new[] { "J.R.R. Tolkien" },
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 30, $"Expected score >= 30, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Exact primary author")), 
            $"Expected 'Exact primary author' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    [Fact]
    public void RankCandidates_SurnameMatch_Scores20()
    {
        var query = new ParsedQuery
        {
            AuthorCandidates = new[] { "Tolkien" }
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            PrimaryAuthors = new[] { "J.R.R. Tolkien" },
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 20 && result[0].Score < 30,
            $"Expected score 20-29, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("surname match")), 
            $"Expected 'surname match' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    [Fact]
    public void RankCandidates_ContributorAuthor_Scores15()
    {
        var query = new ParsedQuery
        {
            AuthorCandidates = new[] { "Alan Lee" }
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit: Illustrated",
            PrimaryAuthors = new[] { "J.R.R. Tolkien" },
            ContributorAuthors = new[] { "Alan Lee" }
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 15 && result[0].Score < 30,
            $"Expected score 15-29, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Contributor author match")), 
            $"Expected 'Contributor author match' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    #endregion

    #region Year Scoring Tests

    [Fact]
    public void RankCandidates_ExactYearMatch_Scores5()
    {
        var query = new ParsedQuery
        {
            YearHint = 1937
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            FirstPublishYear = 1937,
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 5, $"Expected score >= 5, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Year matches")), 
            $"Expected 'Year matches' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    [Fact]
    public void RankCandidates_CloseYearMatch_Scores2()
    {
        var query = new ParsedQuery
        {
            YearHint = 1937
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            FirstPublishYear = 1938, // Within 2 years
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 2, $"Expected score >= 2, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Year close")), 
            $"Expected 'Year close' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    [Fact]
    public void RankCandidates_FarYearMatch_Scores0()
    {
        var query = new ParsedQuery
        {
            YearHint = 1937
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            FirstPublishYear = 1950, // More than 2 years away
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(!result[0].MatchReasons.Any(r => r.Contains("Year")), 
            $"Expected no 'Year' in reasons");
    }

    #endregion

    #region Edition Count Scoring Tests

    [Fact]
    public void RankCandidates_HighEditionCount_Scores3()
    {
        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            EditionCount = 100,
            PrimaryAuthors = Array.Empty<string>(),
            ContributorAuthors = Array.Empty<string>()
        };

        var query = new ParsedQuery { TitleCandidates = new[] { "Hobbit" } };
        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.True(result[0].MatchReasons.Any(r => r.Contains("canonical status")), 
            $"Expected 'canonical status' in reasons, got: {string.Join(", ", result[0].MatchReasons)}");
    }

    #endregion

    #region Ranking Order Tests

    [Fact]
    public void RankCandidates_ReturnsOrderedByScore()
    {
        var candidates = new[]
        {
            new BookCandidate
            {
                Title = "The Hobbit",
                PrimaryAuthors = new[] { "J.R.R. Tolkien" },
                ContributorAuthors = Array.Empty<string>(),
                EditionCount = 50
            },
            new BookCandidate
            {
                Title = "The Hobbit: Illustrated",
                PrimaryAuthors = new[] { "J.R.R. Tolkien" },
                ContributorAuthors = Array.Empty<string>(),
                EditionCount = 0
            },
            new BookCandidate
            {
                Title = "A Completely Different Book",
                PrimaryAuthors = new[] { "Someone Else" },
                ContributorAuthors = Array.Empty<string>(),
                EditionCount = 0
            }
        };

        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "Hobbit" },
            AuthorCandidates = new[] { "Tolkien" }
        };

        var result = _matcher.RankCandidates(query, candidates);

        var scores = result.Select(e => e.Score).ToList();
        var expectedOrder = scores.OrderByDescending(s => s).ToList();
        
        for (int i = 0; i < scores.Count; i++)
        {
            Assert.Equal(expectedOrder[i], scores[i]);
        }
    }

    [Fact]
    public void RankCandidates_MultipleMatches_ReturnsHighestScoresFirst()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "The Hobbit" },
            AuthorCandidates = new[] { "Tolkien" }
        };

        var hobbitExact = new BookCandidate
        {
            Title = "The Hobbit",
            NormalizedTitle = "the hobbit",
            PrimaryAuthors = new[] { "J.R.R. Tolkien" },
            ContributorAuthors = Array.Empty<string>(),
            EditionCount = 100,
            FirstPublishYear = 1937
        };

        var hobbitWithSubtitle = new BookCandidate
        {
            Title = "The Hobbit: Or There and Back Again",
            NormalizedTitle = "the hobbit or there and back again",
            PrimaryAuthors = new[] { "J.R.R. Tolkien" },
            ContributorAuthors = Array.Empty<string>(),
            EditionCount = 80,
            FirstPublishYear = 1937
        };

        var result = _matcher.RankCandidates(query, new[] { hobbitExact, hobbitWithSubtitle });

        // Exact match should score higher than starts-with
        Assert.Equal(2, result.Count);
        Assert.True(result[0].Score > result[1].Score,
            $"Exact match ({result[0].Score}) should score higher than starts-with ({result[1].Score})");
    }

    #endregion

    #region Real-World Examples

    [Fact]
    public void RankCandidates_ChallengeExample_MarkHuckleberry()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "huckleberry", "mark huckleberry" },
            AuthorCandidates = new[] { "mark twain" },
            Keywords = new[] { "finn" }
        };

        var candidate = new BookCandidate
        {
            Title = "Adventures of Huckleberry Finn",
            NormalizedTitle = "adventures of huckleberry finn",
            PrimaryAuthors = new[] { "Mark Twain" },
            ContributorAuthors = Array.Empty<string>(),
            FirstPublishYear = 1884,
            EditionCount = 200
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score > 50, "Should be a decent match");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Exact primary author")), 
            $"Expected 'Exact primary author' in reasons");
        // Note: Title contains match (35) beats token overlap (12), so we get contains, not overlap
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Title contains")), 
            $"Expected 'Title contains' in reasons");
    }

    [Fact]
    public void RankCandidates_ChallengeExample_TolkienHobbit()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = new[] { "hobbit", "the hobbit" },
            AuthorCandidates = new[] { "tolkien" },
            Keywords = new[] { "illustrated", "deluxe" },
            YearHint = 1937
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            NormalizedTitle = "the hobbit",
            PrimaryAuthors = new[] { "J.R.R. Tolkien" },
            ContributorAuthors = Array.Empty<string>(),
            FirstPublishYear = 1937,
            EditionCount = 150
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.True(result[0].Score >= 88, $"Should be a very strong match, got {result[0].Score}");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Exact title match")), 
            $"Expected 'Exact title match' in reasons");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("surname match")), 
            $"Expected 'surname match' in reasons");
        Assert.True(result[0].MatchReasons.Any(r => r.Contains("Year matches")), 
            $"Expected 'Year matches' in reasons");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RankCandidates_EmptyQuery_ReturnsZeroScore()
    {
        var query = new ParsedQuery
        {
            TitleCandidates = Array.Empty<string>(),
            AuthorCandidates = Array.Empty<string>()
        };

        var candidate = new BookCandidate
        {
            Title = "The Hobbit",
            PrimaryAuthors = new[] { "Tolkien" },
            ContributorAuthors = Array.Empty<string>()
        };

        var result = _matcher.RankCandidates(query, new[] { candidate });

        Assert.Single(result);
        Assert.Equal(0, result[0].Score);
    }

    [Fact]
    public void RankCandidates_NullQuery_Throws()
    {
        var candidates = new[] { new BookCandidate { Title = "Test" } };
        Assert.Throws<ArgumentNullException>(() => 
            _matcher.RankCandidates(null!, candidates));
    }

    [Fact]
    public void RankCandidates_NullCandidates_Throws()
    {
        var query = new ParsedQuery();
        Assert.Throws<ArgumentNullException>(() => 
            _matcher.RankCandidates(query, null!));
    }

    [Fact]
    public void RankCandidates_NoCandidates_ReturnsEmpty()
    {
        var query = new ParsedQuery { TitleCandidates = new[] { "Test" } };
        var result = _matcher.RankCandidates(query, Array.Empty<BookCandidate>());

        Assert.Empty(result);
    }

    #endregion
}

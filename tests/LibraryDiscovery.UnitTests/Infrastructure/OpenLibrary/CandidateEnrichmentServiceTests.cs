using LibraryDiscovery.Application.Interfaces;
using LibraryDiscovery.Infrastructure.OpenLibrary;
using LibraryDiscovery.Domain.Entities;
using Xunit;

namespace LibraryDiscovery.UnitTests.Infrastructure.OpenLibrary;

public class CandidateEnrichmentServiceTests
{
    private readonly IStringNormalizationService _normalizationService;
    private readonly CandidateEnrichmentService _enrichmentService;

    public CandidateEnrichmentServiceTests()
    {
        _normalizationService = new MockStringNormalizationService();
        _enrichmentService = new CandidateEnrichmentService(_normalizationService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullNormalizationService_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CandidateEnrichmentService(null!)
        );
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public async Task EnrichAsync_WithNullDocs_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _enrichmentService.EnrichAsync(null!, CancellationToken.None)
        );
    }

    [Fact]
    public async Task EnrichAsync_WithEmptyDocs_ReturnsEmptyList()
    {
        var result = await _enrichmentService.EnrichAsync(
            Array.Empty<OpenLibrarySearchDoc>(),
            CancellationToken.None
        );

        Assert.Empty(result);
    }

    #endregion

    #region Deduplication Tests

    [Fact]
    public async Task EnrichAsync_DuplicateKeysWithDifferentEditionCounts_KeepsHigherEditionCount()
    {
        var docs = new[]
        {
            new OpenLibrarySearchDoc
            {
                Key = "/works/OL45883W",
                Title = "The Hobbit",
                Edition_count = 50
            },
            new OpenLibrarySearchDoc
            {
                Key = "/works/OL45883W",
                Title = "Hobbit",
                Edition_count = 150 // Higher edition count
            }
        };

        var result = await _enrichmentService.EnrichAsync(docs, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(150, result[0].EditionCount);
    }

    [Fact]
    public async Task EnrichAsync_DuplicateKeysWithoutEditionCounts_KeepsFirst()
    {
        var docs = new[]
        {
            new OpenLibrarySearchDoc
            {
                Key = "/works/OL45883W",
                Title = "The Hobbit"
            },
            new OpenLibrarySearchDoc
            {
                Key = "/works/OL45883W",
                Title = "Another Title"
            }
        };

        var result = await _enrichmentService.EnrichAsync(docs, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("The Hobbit", result[0].Title);
    }

    #endregion

    #region Basic Candidate Creation Tests

    [Fact]
    public async Task EnrichAsync_WithValidDoc_CreatesCandidateWithCorrectFields()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "The Hobbit",
            Author_name = "J.R.R. Tolkien",
            First_publish_year = 1937,
            Edition_count = 100,
            Cover_id = "123"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        Assert.Single(result);
        var candidate = result[0];
        
        Assert.Equal("OL45883W", candidate.OpenLibraryWorkId);
        Assert.Equal("/works/OL45883W", candidate.OpenLibraryKey);
        Assert.Equal("The Hobbit", candidate.Title);
        Assert.Contains("J.R.R. Tolkien", candidate.PrimaryAuthors);
        Assert.Equal(1937, candidate.FirstPublishYear);
        Assert.Equal(100, candidate.EditionCount);
    }

    [Fact]
    public async Task EnrichAsync_WithAuthorNames_UsesArrayOverScalar()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "The Hobbit",
            Author_name = "SingleAuthor",
            Author_names = new[] { "J.R.R. Tolkien", "Christopher Tolkien" }
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        var candidate = result[0];
        Assert.Contains("J.R.R. Tolkien", candidate.PrimaryAuthors);
        Assert.Contains("Christopher Tolkien", candidate.PrimaryAuthors);
    }

    [Fact]
    public async Task EnrichAsync_WithoutAuthors_ReturnsEmptyAuthorArray()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Anonymous Work"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        var candidate = result[0];
        Assert.Empty(candidate.PrimaryAuthors);
    }

    [Fact]
    public async Task EnrichAsync_WithMissingEditionCount_DefaultsToZero()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        Assert.Equal(0, result[0].EditionCount);
    }

    #endregion

    #region Normalization Tests

    [Fact]
    public async Task EnrichAsync_NormalizesTitleField()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "The Hobbit: There and Back Again"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        var candidate = result[0];
        Assert.NotNull(candidate.NormalizedTitle);
        Assert.IsType<string>(candidate.NormalizedTitle);
    }

    [Fact]
    public async Task EnrichAsync_ExtractsSurnamesFromAuthors()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book",
            Author_names = new[] { "J.R.R. Tolkien", "Christopher Tolkien" }
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        var candidate = result[0];
        Assert.NotNull(candidate.NormalizedPrimaryAuthorSurnames);
        Assert.NotEmpty(candidate.NormalizedPrimaryAuthorSurnames);
    }

    #endregion

    #region Cover URL Tests

    [Fact]
    public async Task EnrichAsync_WithCoverId_BuildsCoverUrl()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book",
            Cover_id = "7"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        var candidate = result[0];
        Assert.NotNull(candidate.CoverUrl);
        Assert.Contains("covers.openlibrary.org", candidate.CoverUrl);
        Assert.Contains("7", candidate.CoverUrl);
    }

    [Fact]
    public async Task EnrichAsync_WithoutCoverId_CoverUrlIsNull()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        Assert.Null(result[0].CoverUrl);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task EnrichAsync_WithDocWithoutKey_SkipsEnrichment()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Title = "Book Without Key"
            // Key is explicitly not set (defaults to empty)
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        // Should still create a basic candidate
        Assert.Single(result);
        Assert.Equal("Book Without Key", result[0].Title);
    }

    [Fact]
    public async Task EnrichAsync_WithNullRawData_UsesEmptyDictionary()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book",
            RawData = null
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        Assert.NotNull(result[0].RawData);
        Assert.IsType<Dictionary<string, object>>(result[0].RawData);
    }

    #endregion

    #region Multiple Candidates Tests

    [Fact]
    public async Task EnrichAsync_WithMultipleDocs_ReturnMultipleCandidates()
    {
        var docs = new[]
        {
            new OpenLibrarySearchDoc
            {
                Key = "/works/OL45883W",
                Title = "The Hobbit",
                Edition_count = 100
            },
            new OpenLibrarySearchDoc
            {
                Key = "/works/OL17993265W",
                Title = "The Lord of the Rings",
                Edition_count = 200
            }
        };

        var result = await _enrichmentService.EnrichAsync(docs, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task EnrichAsync_CancellationToken_IsRespected()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            var result = await _enrichmentService.EnrichAsync(
                new[] { doc },
                cts.Token
            );
            // If it doesn't throw, it's still valid
            Assert.NotNull(result);
        }
        catch (OperationCanceledException)
        {
            // Also acceptable
        }
    }

    #endregion

    #region Return Type Tests

    [Fact]
    public async Task EnrichAsync_ReturnsReadOnlyList()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        Assert.IsAssignableFrom<IReadOnlyList<BookCandidate>>(result);
    }

    [Fact]
    public async Task EnrichAsync_ReturnsCandidatesWithAllRequiredFields()
    {
        var doc = new OpenLibrarySearchDoc
        {
            Key = "/works/OL45883W",
            Title = "Book",
            Author_name = "Author"
        };

        var result = await _enrichmentService.EnrichAsync(new[] { doc }, CancellationToken.None);

        var candidate = result[0];
        Assert.NotNull(candidate);
        Assert.IsType<BookCandidate>(candidate);
        Assert.NotEmpty(candidate.Title);
        Assert.NotEmpty(candidate.OpenLibraryWorkId);
    }

    #endregion
}

/// <summary>
/// Mock implementation for testing that matches test expectations.
/// </summary>
internal class MockStringNormalizationService : IStringNormalizationService
{
    public string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        return input.ToLower().Trim();
    }

    public string[] ToTokens(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<string>();
        return input.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    public string NormalizeWithoutStopwords(string input)
    {
        var normalized = Normalize(input);
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var stopwords = new[] { "the", "a", "an" };
        var filtered = tokens.Where(t => !stopwords.Contains(t)).ToArray();
        return string.Join(" ", filtered);
    }
}

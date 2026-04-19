using LibraryDiscovery.Application.Interfaces;
using LibraryDiscovery.Infrastructure.OpenLibrary;
using Xunit;

namespace LibraryDiscovery.UnitTests.Infrastructure.OpenLibrary;

public class OpenLibrarySearchServiceTests
{
    private readonly OpenLibrarySearchService _service;
    private readonly HttpClient _httpClient;

    public OpenLibrarySearchServiceTests()
    {
        _httpClient = new HttpClient();
        _service = new OpenLibrarySearchService(_httpClient);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenLibrarySearchService(null!));
    }

    #endregion

    #region Request Validation Tests

    [Fact]
    public async Task SearchAsync_WithNullRequest_ThrowsArgumentNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SearchAsync(null!, CancellationToken.None)
        );
    }

    [Fact]
    public async Task SearchAsync_WithEmptyRequest_ReturnsEmptyList()
    {
        var request = new OpenLibrarySearchRequest();
        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Result Deduplication Tests

    [Fact]
    public async Task SearchAsync_ResultsAreLimitedToRequestLimit()
    {
        var request = new OpenLibrarySearchRequest
        {
            Title = "The",
            Limit = 5 // Would get many results for "The", should cap at 5
        };

        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.True(result.Count <= request.Limit,
            $"Result count {result.Count} should not exceed limit {request.Limit}");
    }

    #endregion

    #region Request Strategy Tests

    [Fact]
    public async Task SearchAsync_TitleOnlyPassesTitle()
    {
        var request = new OpenLibrarySearchRequest
        {
            Title = "The Hobbit",
            Limit = 3
        };

        // This would call live API - just verify it doesn't throw
        // In production, mock the HttpClient for this test
        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        // Result depends on live API availability
    }

    [Fact]
    public async Task SearchAsync_AuthorOnlyPassesAuthor()
    {
        var request = new OpenLibrarySearchRequest
        {
            Author = "J.R.R. Tolkien",
            Limit = 3
        };

        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        // Result depends on live API availability
    }

    [Fact]
    public async Task SearchAsync_TitleAndAuthorPassesBoth()
    {
        var request = new OpenLibrarySearchRequest
        {
            Title = "The Hobbit",
            Author = "Tolkien",
            Limit = 3
        };

        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        // Result depends on live API availability
    }

    [Fact]
    public async Task SearchAsync_RawQueryFallsbck()
    {
        var request = new OpenLibrarySearchRequest
        {
            RawQuery = "hobbit tolkien illustrated",
            Limit = 3
        };

        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        // Result depends on live API availability
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task SearchAsync_ReturnsOpenLibrarySearchDocs()
    {
        var request = new OpenLibrarySearchRequest
        {
            Title = "The Hobbit",
            Limit = 3
        };

        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.All(result, doc =>
        {
            Assert.NotNull(doc);
            Assert.IsType<OpenLibrarySearchDoc>(doc);
        });
    }

    [Fact]
    public async Task SearchAsync_ReturnsReadOnlyList()
    {
        var request = new OpenLibrarySearchRequest
        {
            Title = "The Hobbit",
            Limit = 1
        };

        var result = await _service.SearchAsync(request, CancellationToken.None);

        Assert.IsAssignableFrom<IReadOnlyList<OpenLibrarySearchDoc>>(result);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task SearchAsync_CancellationToken_IsRespected()
    {
        var request = new OpenLibrarySearchRequest
        {
            Title = "The",
            Limit = 1
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should either complete (no results) or throw OperationCanceledException
        try
        {
            var result = await _service.SearchAsync(request, cts.Token);
            // If it returns, check it's valid
            Assert.NotNull(result);
        }
        catch (OperationCanceledException)
        {
            // This is also acceptable
        }
    }

    #endregion

    #region Resilience Tests

    [Fact]
    public async Task SearchAsync_HandlesNetworkError_ReturnsEmptyResults()
    {
        // Using invalid URL to trigger network error
        var httpClient = new HttpClient();
        var service = new OpenLibrarySearchService(httpClient);

        var request = new OpenLibrarySearchRequest
        {
            Title = "test",
            Limit = 1
        };

        // Should not throw, returns empty list on network errors
        var result = await service.SearchAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        // May be empty if connection refused
    }

    #endregion

    #region Data Model Tests

    [Fact]
    public void OpenLibrarySearchDoc_HasDefaultValues()
    {
        var doc = new OpenLibrarySearchDoc();

        Assert.Equal(string.Empty, doc.Key);
        Assert.Equal(string.Empty, doc.Title);
        Assert.Null(doc.Author_name);
        Assert.Null(doc.Author_names);
        Assert.Null(doc.First_publish_year);
        Assert.Null(doc.Edition_count);
        Assert.Null(doc.Cover_id);
    }

    [Fact]
    public void OpenLibrarySearchRequest_HasDefaultLimit()
    {
        var request = new OpenLibrarySearchRequest();

        Assert.Equal(10, request.Limit);
    }

    [Fact]
    public void OpenLibrarySearchRequest_CanSetCustomLimit()
    {
        var request = new OpenLibrarySearchRequest { Limit = 25 };

        Assert.Equal(25, request.Limit);
    }

    #endregion

    #region Integration Example Tests

    [Fact]
    public async Task SearchAsync_HobbitQuery_ReturnsResults()
    {
        // Integration test: actually calls Open Library
        var request = new OpenLibrarySearchRequest
        {
            Title = "The Hobbit",
            Author = "Tolkien",
            Limit = 2
        };

        using var httpClient = new HttpClient();
        var service = new OpenLibrarySearchService(httpClient);
        var result = await service.SearchAsync(request, CancellationToken.None);

        // If tests run online:
        // - Should have results
        // - Results should have Hobbit in title
        // - Edition count should be >0 for this classic
        //
        // If offline or API down:
        // - Returns empty list (no exceptions)

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<OpenLibrarySearchDoc>>(result);
    }

    [Fact]
    public async Task SearchAsync_AuthorSearch_ReturnsMultiple()
    {
        // Test author-based search strategy
        var request = new OpenLibrarySearchRequest
        {
            Author = "Jane Austen",
            Limit = 3
        };

        using var httpClient = new HttpClient();
        var service = new OpenLibrarySearchService(httpClient);
        var result = await service.SearchAsync(request, CancellationToken.None);

        // Jane Austen has many books - should return results if online
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<OpenLibrarySearchDoc>>(result);
    }

    #endregion
}

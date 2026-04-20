using LibraryDiscovery.Application.DTOs;
using LibraryDiscovery.Application.Interfaces;
using LibraryDiscovery.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryDiscovery.UnitTests.Controllers;

public class BooksControllerTests
{
    // --- Stub ---

    private sealed class StubBookMatchService : IBookMatchService
    {
        private readonly BookMatchResponse _response;

        public StubBookMatchService(BookMatchResponse response) => _response = response;

        public Task<BookMatchResponse> MatchAsync(string rawQuery, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }

    // --- Helpers ---

    private static BooksController CreateController(BookMatchResponse response)
        => new(new StubBookMatchService(response), NullLogger<BooksController>.Instance);

    private static BookMatchResponse EmptyResponse(string query = "test") => new()
    {
        Query = query,
        Matches = Array.Empty<BookMatchResultDto>()
    };

    private static BookMatchResponse ResponseWithMatches(params BookMatchResultDto[] matches) => new()
    {
        Query = "tolkien hobbit",
        Matches = matches
    };

    // --- Tests ---

    [Fact]
    public async Task MatchBooks_NullRequest_ReturnsBadRequest()
    {
        var controller = CreateController(EmptyResponse());

        var result = await controller.MatchBooks(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MatchBooks_EmptyOrWhitespaceQuery_ReturnsBadRequest(string query)
    {
        var controller = CreateController(EmptyResponse());

        var result = await controller.MatchBooks(new BookMatchRequest { Query = query }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task MatchBooks_ValidQuery_ReturnsOk()
    {
        var controller = CreateController(EmptyResponse("dickens"));

        var result = await controller.MatchBooks(new BookMatchRequest { Query = "dickens" }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task MatchBooks_ValidQuery_ReturnsServiceResponse()
    {
        var match = new BookMatchResultDto { Title = "The Hobbit", Score = 95 };
        var controller = CreateController(ResponseWithMatches(match));

        var result = await controller.MatchBooks(
            new BookMatchRequest { Query = "tolkien hobbit" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BookMatchResponse>(ok.Value);
        Assert.Single(response.Matches);
        Assert.Equal("The Hobbit", response.Matches[0].Title);
    }

    [Fact]
    public async Task MatchBooks_NoMatches_ReturnsOkWithEmptyList()
    {
        var controller = CreateController(EmptyResponse("unknownxyz"));

        var result = await controller.MatchBooks(
            new BookMatchRequest { Query = "unknownxyz" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BookMatchResponse>(ok.Value);
        Assert.Empty(response.Matches);
    }

    [Fact]
    public async Task MatchBooks_ServiceReturnsMessage_ReturnsOkWithMessage()
    {
        var errorResponse = new BookMatchResponse
        {
            Query = "bad input",
            Matches = Array.Empty<BookMatchResultDto>(),
            Message = "Error during matching: timeout"
        };
        var controller = CreateController(errorResponse);

        var result = await controller.MatchBooks(
            new BookMatchRequest { Query = "bad input" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BookMatchResponse>(ok.Value);
        Assert.Equal("Error during matching: timeout", response.Message);
    }

    [Fact]
    public async Task MatchBooks_CancellationRequested_PropagatesToken()
    {
        using var cts = new CancellationTokenSource();
        var tokenPassed = CancellationToken.None;

        var capturingStub = new CapturingStub(t => { tokenPassed = t; return EmptyResponse(); });
        var controller = new BooksController(capturingStub, NullLogger<BooksController>.Instance);

        cts.Cancel();
        await controller.MatchBooks(new BookMatchRequest { Query = "test" }, cts.Token);

        Assert.True(tokenPassed.IsCancellationRequested);
    }

    private sealed class CapturingStub : IBookMatchService
    {
        private readonly Func<CancellationToken, BookMatchResponse> _fn;
        public CapturingStub(Func<CancellationToken, BookMatchResponse> fn) => _fn = fn;
        public Task<BookMatchResponse> MatchAsync(string rawQuery, CancellationToken ct)
            => Task.FromResult(_fn(ct));
    }
}

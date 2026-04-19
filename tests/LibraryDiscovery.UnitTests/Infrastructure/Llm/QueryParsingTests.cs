using LibraryDiscovery.Infrastructure.Llm;
using LibraryDiscovery.Domain.ValueObjects;
using Xunit;

namespace LibraryDiscovery.UnitTests.Infrastructure.Llm;

public class FallbackQueryParserTests
{
    private readonly FallbackQueryParser _parser = new();

    #region Basic Parsing Tests

    [Fact]
    public void Parse_SimpleTitleByAuthor_ExtractsCorrectly()
    {
        var result = _parser.Parse("The Hobbit by J.R.R. Tolkien");

        Assert.NotEmpty(result.TitleCandidates);
        Assert.Contains("The Hobbit", result.TitleCandidates);
        Assert.NotEmpty(result.AuthorCandidates);
        Assert.Contains("J.R.R. Tolkien", result.AuthorCandidates);
    }

    [Fact]
    public void Parse_TitleWithAuthorIndicator_SplitsOnBy()
    {
        var result = _parser.Parse("Adventures of Huckleberry Finn by Mark Twain");

        Assert.Contains("Adventures of Huckleberry Finn", result.TitleCandidates);
        Assert.Contains("Mark Twain", result.AuthorCandidates);
    }

    [Fact]
    public void Parse_MessyQuery_ExtractsAllInfo()
    {
        var result = _parser.Parse("tolkien hobbit illustrated deluxe 1937");

        Assert.NotEmpty(result.TitleCandidates);
        Assert.Contains("illustrated", result.Keywords);
        Assert.Contains("deluxe", result.Keywords);
        Assert.Equal(1937, result.YearHint);
    }

    [Fact]
    public void Parse_QuotedTitle_ExtractsFromQuotes()
    {
        var result = _parser.Parse("Looking for \"The Hobbit\" by Tolkien");

        Assert.Contains("The Hobbit", result.TitleCandidates);
    }

    #endregion

    #region Year Extraction Tests

    [Fact]
    public void Parse_YearInParentheses_ExtractsYear()
    {
        var result = _parser.Parse("The Hobbit (1937)");

        Assert.Equal(1937, result.YearHint);
    }

    [Fact]
    public void Parse_YearAtEndOfQuery_ExtractsYear()
    {
        var result = _parser.Parse("Hobbit 1937");

        Assert.Equal(1937, result.YearHint);
    }

    [Fact]
    public void Parse_InvalidYear_ReturnsNull()
    {
        var result = _parser.Parse("Hobbit 2050");

        Assert.Null(result.YearHint);
    }

    [Fact]
    public void Parse_NoYear_ReturnsNull()
    {
        var result = _parser.Parse("The Hobbit");

        Assert.Null(result.YearHint);
    }

    #endregion

    #region Keyword Extraction Tests

    [Fact]
    public void Parse_ContainsIllustrated_ExtractsKeyword()
    {
        var result = _parser.Parse("Hobbit illustrated edition");

        Assert.Contains("illustrated", result.Keywords);
    }

    [Fact]
    public void Parse_MultipleKeywords_ExtractsAll()
    {
        var result = _parser.Parse("deluxe hardcover anniversary edition");

        Assert.Contains("deluxe", result.Keywords);
        Assert.Contains("hardcover", result.Keywords);
        Assert.Contains("anniversary", result.Keywords);
    }

    [Fact]
    public void Parse_NoKeywords_ReturnsEmpty()
    {
        var result = _parser.Parse("The Simple Hobbit");

        Assert.Empty(result.Keywords);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyResult()
    {
        var result = _parser.Parse(string.Empty);

        Assert.Empty(result.TitleCandidates);
        Assert.Empty(result.AuthorCandidates);
        Assert.Null(result.YearHint);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmptyResult()
    {
        var result = _parser.Parse("   ");

        Assert.Empty(result.TitleCandidates);
    }

    [Fact]
    public void Parse_CaseInsensitive_NormalizesToOriginalCase()
    {
        var result = _parser.Parse("TOLKIEN HOBBIT ILLUSTRATED");

        // Keywords should still be extracted despite uppercase
        Assert.Contains("illustrated", result.Keywords);
    }

    [Fact]
    public void Parse_DuplicateExtraction_RemoveDuplicates()
    {
        var result = _parser.Parse("Hobbit by Tolkien, Hobbit by Tolkien");

        // Should contain title/author once even if query mentions twice
        Assert.Single(result.TitleCandidates.Where(t => t.Contains("Hobbit")));
    }

    #endregion

    #region Complex Author Patterns

    [Fact]
    public void Parse_AuthorIndicatorFrom_SplitsCorrectly()
    {
        var result = _parser.Parse("A famous work from Mark Twain");

        Assert.NotEmpty(result.AuthorCandidates);
        Assert.Contains("Mark Twain", result.AuthorCandidates);
    }

    #endregion
}

public class GeminiQueryParsingServiceTests
{
    private readonly FallbackQueryParser _fallbackParser = new();

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GeminiQueryParsingService(null!, _fallbackParser, "key")
        );
    }

    [Fact]
    public void Constructor_WithNullFallbackParser_ThrowsArgumentNull()
    {
        var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new GeminiQueryParsingService(httpClient, null!, "key")
        );
    }

    [Fact]
    public async Task ParseAsync_EmptyQuery_ReturnsParsedQueryWithEmpty()
    {
        var httpClient = new HttpClient();
        var service = new GeminiQueryParsingService(httpClient, _fallbackParser, null);

        var result = await service.ParseAsync(string.Empty, CancellationToken.None);

        Assert.Empty(result.TitleCandidates);
        Assert.Empty(result.AuthorCandidates);
    }

    [Fact]
    public async Task ParseAsync_NoApiKeyConfigured_UsesFallback()
    {
        var httpClient = new HttpClient();
        var service = new GeminiQueryParsingService(httpClient, _fallbackParser, (string)null!);

        var result = await service.ParseAsync("Hobbit by Tolkien", CancellationToken.None);

        // Should use fallback parser
        Assert.NotNull(result.ConfidenceNotes);
        Assert.Contains("fallback", result.ConfidenceNotes!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAsync_WithoutValidApiKey_UsesFallback()
    {
        var httpClient = new HttpClient();
        var fallback = new FallbackQueryParser();
        var service = new GeminiQueryParsingService(httpClient, fallback, "invalid-key-for-test");

        // This would attempt API call and fail, then use fallback
        var result = await service.ParseAsync("The Hobbit", CancellationToken.None);

        // Should have fallback notes
        Assert.NotNull(result.ConfidenceNotes);
        Assert.Contains("fallback", result.ConfidenceNotes, StringComparison.OrdinalIgnoreCase);
    }
}

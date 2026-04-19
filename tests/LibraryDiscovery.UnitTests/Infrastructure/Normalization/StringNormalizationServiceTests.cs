using LibraryDiscovery.Infrastructure.Normalization;
using Xunit;

namespace LibraryDiscovery.UnitTests.Infrastructure.Normalization;

public class StringNormalizationServiceTests
{
    private readonly StringNormalizationService _service = new();

    #region Normalize Tests

    [Fact]
    public void Normalize_WithNull_ReturnsEmpty()
    {
        var result = _service.Normalize(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WithEmpty_ReturnsEmpty()
    {
        var result = _service.Normalize(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WithWhitespace_ReturnsEmpty()
    {
        var result = _service.Normalize("   ");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_ConvertsTolowercase()
    {
        var result = _service.Normalize("THE HOBBIT");
        Assert.Equal("the hobbit", result);
    }

    [Fact]
    public void Normalize_TrimsBoundaryWhitespace()
    {
        var result = _service.Normalize("  The Hobbit  ");
        Assert.Equal("the hobbit", result);
    }

    [Fact]
    public void Normalize_RemovesPointuation()
    {
        var result = _service.Normalize("The Hobbit: Or There and Back Again");
        Assert.Equal("the hobbit or there and back again", result);
    }

    [Fact]
    public void Normalize_CollapsesMultipleSpaces()
    {
        var result = _service.Normalize("The    Hobbit   ");
        Assert.Equal("the hobbit", result);
    }

    [Fact]
    public void Normalize_RemovesDiacritics()
    {
        var result = _service.Normalize("Café");
        Assert.Equal("cafe", result);
    }

    [Fact]
    public void Normalize_RemovesDiacriticsAcrossLanguages()
    {
        // Jürgen -> Jurgen
        var result = _service.Normalize("Jürgen Müller");
        Assert.Equal("jurgen muller", result);
    }

    [Fact]
    public void Normalize_HandleToLkienExample()
    {
        // Full realistic example from the challenge
        var result = _service.Normalize("The Hobbit: Or There and Back Again");
        Assert.Equal("the hobbit or there and back again", result);
    }

    [Fact]
    public void Normalize_HandleMultipleQuotes()
    {
        var result = _service.Normalize("'The Hobbit' - \"Illustrated Edition\"");
        Assert.Equal("the hobbit illustrated edition", result);
    }

    [Fact]
    public void Normalize_HandleCommasAndParentheses()
    {
        var result = _service.Normalize("Twain, Mark (Author)");
        Assert.Equal("twain mark author", result);
    }

    #endregion

    #region ToTokens Tests

    [Fact]
    public void ToTokens_SplitsOnWhitespace()
    {
        var result = _service.ToTokens("the hobbit");
        Assert.Equal(new[] { "the", "hobbit" }, result);
    }

    [Fact]
    public void ToTokens_NormalizesThenSplits()
    {
        var result = _service.ToTokens("The HOBBIT");
        Assert.Equal(new[] { "the", "hobbit" }, result);
    }

    [Fact]
    public void ToTokens_RemovesPunctuationBeforeSplitting()
    {
        var result = _service.ToTokens("The Hobbit: There and Back Again");
        Assert.Equal(new[] { "the", "hobbit", "there", "and", "back", "again" }, result);
    }

    [Fact]
    public void ToTokens_HandlesMultipleSpaces()
    {
        var result = _service.ToTokens("the    hobbit");
        Assert.Equal(new[] { "the", "hobbit" }, result);
    }

    [Fact]
    public void ToTokens_WithEmpty_ReturnsEmptyArray()
    {
        var result = _service.ToTokens(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void ToTokens_WithComplexString()
    {
        var result = _service.ToTokens("J.R.R. Tolkien's 'The Hobbit' (1937)");
        // Periods become spaces, so J.R.R. becomes individual 'j', 'r', 'r' tokens
        Assert.Equal(new[] { "j", "r", "r", "tolkien", "s", "the", "hobbit", "1937" }, result);
    }

    #endregion

    #region NormalizeWithoutStopwords Tests

    [Fact]
    public void NormalizeWithoutStopwords_RemovesThe()
    {
        var result = _service.NormalizeWithoutStopwords("The Hobbit");
        Assert.Equal("hobbit", result);
    }

    [Fact]
    public void NormalizeWithoutStopwords_RemovesA()
    {
        var result = _service.NormalizeWithoutStopwords("A Tale of Two Cities");
        Assert.Equal("tale of two cities", result);
    }

    [Fact]
    public void NormalizeWithoutStopwords_RemovesAn()
    {
        var result = _service.NormalizeWithoutStopwords("An American Tragedy");
        Assert.Equal("american tragedy", result);
    }

    [Fact]
    public void NormalizeWithoutStopwords_RemovesAllStopwordsAtOnce()
    {
        var result = _service.NormalizeWithoutStopwords("The Adventures of a Boy");
        Assert.Equal("adventures of boy", result);
    }

    [Fact]
    public void NormalizeWithoutStopwords_PreservesNonStopwords()
    {
        var result = _service.NormalizeWithoutStopwords("Pride and Prejudice");
        Assert.Equal("pride and prejudice", result);
    }

    [Fact]
    public void NormalizeWithoutStopwords_CaseInsensitive()
    {
        var result = _service.NormalizeWithoutStopwords("THE HOBBIT");
        Assert.Equal("hobbit", result);
    }

    [Fact]
    public void NormalizeWithoutStopwords_WithEmpty_ReturnsEmpty()
    {
        var result = _service.NormalizeWithoutStopwords(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NormalizeWithoutStopwords_OnlyStopwords_ReturnsEmpty()
    {
        var result = _service.NormalizeWithoutStopwords("The a an");
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Real-World Examples

    [Theory]
    [InlineData("mark huckleberry", "mark huckleberry")]
    [InlineData("Mark Huckleberry", "mark huckleberry")]
    [InlineData("'Mark Huckleberry'", "mark huckleberry")]
    public void Normalize_HandlesChallengeExamples(string input, string expected)
    {
        var result = _service.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("tolkien hobbit illustrated deluxe 1937", new[] { "tolkien", "hobbit", "illustrated", "deluxe", "1937" })]
    [InlineData("twilight meyer", new[] { "twilight", "meyer" })]
    public void ToTokens_HandlesChallengeExamples(string input, string[] expectedTokens)
    {
        var result = _service.ToTokens(input);
        Assert.Equal(expectedTokens, result);
    }

    [Theory]
    [InlineData("The Hobbit: Or There and Back Again", "hobbit or there and back again")]
    [InlineData("The Adventures of Huckleberry Finn", "adventures of huckleberry finn")]
    [InlineData("A Tale of Two Cities", "tale of two cities")]
    public void NormalizeWithoutStopwords_HandlesTitles(string input, string expected)
    {
        var result = _service.NormalizeWithoutStopwords(input);
        Assert.Equal(expected, result);
    }

    #endregion
}

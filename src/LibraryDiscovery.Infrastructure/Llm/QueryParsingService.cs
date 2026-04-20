namespace LibraryDiscovery.Infrastructure.Llm;

/// <summary>
/// Implementation of IQueryParsingService that uses the fallback (regex/heuristic) parser.
/// This avoids requiring LLM API configuration for basic functionality.
/// </summary>
public class QueryParsingService : IQueryParsingService
{
    private readonly IQueryParsingFallback _fallbackParser;
    private readonly ILogger<QueryParsingService> _logger;

    public QueryParsingService(IQueryParsingFallback fallbackParser, ILogger<QueryParsingService> logger)
    {
        _fallbackParser = fallbackParser ?? throw new ArgumentNullException(nameof(fallbackParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses a messy query using the fallback parser.
    /// </summary>
    public Task<ParsedQuery> ParseAsync(string rawQuery, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
            return Task.FromResult(new ParsedQuery());

        _logger.LogInformation("No Gemini API key configured, falling back to regex parser");
        var result = _fallbackParser.Parse(rawQuery);
        return Task.FromResult(result);
    }
}

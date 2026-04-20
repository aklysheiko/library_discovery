namespace LibraryDiscovery.Application.Services;

/// <summary>
/// Main orchestrator for the book matching workflow.
/// Coordinates: Parse Query → Search → Enrich → Rank → Explain
/// </summary>
public class BookMatchService : IBookMatchService
{
    private readonly IQueryParsingService _queryParser;
    private readonly IOpenLibrarySearchService _searchService;
    private readonly ICandidateEnrichmentService _enrichmentService;
    private readonly IBookMatcher _bookMatcher;
    private readonly IExplanationBuilder _explanationBuilder;
    private readonly IStringNormalizationService _normalizationService;
    private readonly ILogger<BookMatchService> _logger;

    public BookMatchService(
        IQueryParsingService queryParser,
        IOpenLibrarySearchService searchService,
        ICandidateEnrichmentService enrichmentService,
        IBookMatcher bookMatcher,
        IExplanationBuilder explanationBuilder,
        IStringNormalizationService normalizationService,
        ILogger<BookMatchService> logger)
    {
        _queryParser = queryParser ?? throw new ArgumentNullException(nameof(queryParser));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _enrichmentService = enrichmentService ?? throw new ArgumentNullException(nameof(enrichmentService));
        _bookMatcher = bookMatcher ?? throw new ArgumentNullException(nameof(bookMatcher));
        _explanationBuilder = explanationBuilder ?? throw new ArgumentNullException(nameof(explanationBuilder));
        _normalizationService = normalizationService ?? throw new ArgumentNullException(nameof(normalizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Matches a messy user query to candidate books.
    /// Workflow: Parse → Search → Enrich → Rank → Explain
    /// </summary>
    public async Task<BookMatchResponse> MatchAsync(string rawQuery, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
            return CreateEmptyResponse(rawQuery);

        try
        {
            var overallStart = System.Diagnostics.Stopwatch.GetTimestamp();
            _logger.LogInformation("Match start: QueryLen={Len}", rawQuery.Length);
            // Step 1: Parse the raw query into structured format
            var parsedQuery = await _queryParser.ParseAsync(rawQuery, cancellationToken);
            _logger.LogInformation("Parsed query: Titles={Titles}, Authors={Authors}, Keywords={Keywords}, Year={Year}, Notes={Notes}",
                parsedQuery.TitleCandidates.Count, parsedQuery.AuthorCandidates.Count, parsedQuery.Keywords.Count, parsedQuery.YearHint, parsedQuery.ConfidenceNotes);
            
            // Step 2: Create search request from parsed query
            var searchRequest = new OpenLibrarySearchRequest
            {
                Title = parsedQuery.TitleCandidates.FirstOrDefault(),
                Author = parsedQuery.AuthorCandidates.FirstOrDefault(),
                RawQuery = rawQuery,
                Limit = 20
            };

            // Step 3: Search Open Library for candidates
            var searchResults = await _searchService.SearchAsync(searchRequest, cancellationToken);
            _logger.LogInformation("Search returned {Count} docs", searchResults.Count);
            
            if (searchResults.Count == 0)
            {
                _logger.LogWarning("No search results: title='{Title}', author='{Author}'", searchRequest.Title, searchRequest.Author);
                return CreateEmptyResponse(rawQuery);
            }

            // Step 4: Enrich search results with normalized data and deduplication
            var enrichedCandidates = await _enrichmentService.EnrichAsync(searchResults, cancellationToken);
            _logger.LogInformation("Enrichment produced {Count} candidates", enrichedCandidates.Count);
            
            if (enrichedCandidates.Count == 0)
            {
                _logger.LogWarning("No candidates after enrichment");
                return CreateEmptyResponse(rawQuery);
            }

            // Step 5: Rank candidates using deterministic scoring
            var rankedCandidates = _bookMatcher.RankCandidates(parsedQuery, enrichedCandidates);
            if (rankedCandidates.Count > 0)
            {
                var top = rankedCandidates.Take(3).Select(r => $"'{r.Candidate.Title}' ({r.Score})");
                _logger.LogInformation("Top matches: {Top}", string.Join(", ", top));
            }

            // Step 6: Build explanations and convert to DTOs
            var matchResults = rankedCandidates
                .Select(evaluation => ConvertToResultDto(evaluation))
                .ToList();

            var elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - overallStart) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            _logger.LogInformation("Match finished: {Count} results in {Elapsed:n0} ms", matchResults.Count, elapsedMs);
            return new BookMatchResponse
            {
                Query = rawQuery,
                ParsedQuery = ConvertToParsedQueryDto(parsedQuery),
                Matches = matchResults.AsReadOnly()
            };
        }
        catch (Exception ex)
        {
            // Log error and return response indicating failure
            _logger.LogError(ex, "Match failed: {Message}", ex.Message);
            return new BookMatchResponse
            {
                Query = rawQuery,
                Matches = Array.Empty<BookMatchResultDto>(),
                Message = $"Error during matching: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Converts a ranked evaluation into a result DTO with explanation.
    /// </summary>
    private BookMatchResultDto ConvertToResultDto(MatchEvaluation evaluation)
    {
        var explanation = _explanationBuilder.Build(evaluation);

        return new BookMatchResultDto
        {
            Title = evaluation.Candidate.Title,
            PrimaryAuthors = evaluation.Candidate.PrimaryAuthors,
            FirstPublishYear = evaluation.Candidate.FirstPublishYear,
            OpenLibraryWorkId = evaluation.Candidate.OpenLibraryWorkId,
            CoverUrl = evaluation.Candidate.CoverUrl,
            Score = evaluation.Score,
            Explanation = explanation
        };
    }

    /// <summary>
    /// Converts ParsedQuery to DTO for client consumption.
    /// </summary>
    private ParsedQueryDto ConvertToParsedQueryDto(ParsedQuery query)
    {
        return new ParsedQueryDto
        {
            TitleCandidates = query.TitleCandidates.ToArray(),
            AuthorCandidates = query.AuthorCandidates.ToArray(),
            Keywords = query.Keywords.ToArray(),
            YearHint = query.YearHint
        };
    }

    /// <summary>
    /// Creates an empty response when no results are found.
    /// </summary>
    private BookMatchResponse CreateEmptyResponse(string rawQuery)
    {
        return new BookMatchResponse
        {
            Query = rawQuery,
            Matches = Array.Empty<BookMatchResultDto>()
        };
    }
}

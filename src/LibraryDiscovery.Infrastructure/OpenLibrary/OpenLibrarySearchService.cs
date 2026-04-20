using System.Text.Json;
using System.Web;

namespace LibraryDiscovery.Infrastructure.OpenLibrary;

/// <summary>
/// Searches Open Library API for book candidates.
/// Implements multi-strategy search: title+author, title-only, author-only.
/// </summary>
public class OpenLibrarySearchService : IOpenLibrarySearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenLibrarySearchService> _logger;
    private const string OpenLibraryApiBase = "https://openlibrary.org/search.json";

    public OpenLibrarySearchService(HttpClient httpClient, ILogger<OpenLibrarySearchService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches Open Library with fallback multilayerStrategy.
    /// Strategy:
    /// 1. Try title + author search (most specific)
    /// 2. Fall back to title only
    /// 3. Fall back to author only
    /// 4. Fall back to raw query
    /// </summary>
    public async Task<IReadOnlyList<OpenLibrarySearchDoc>> SearchAsync(
        OpenLibrarySearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("OpenLibrary search started: TitleLen={TitleLen}, AuthorLen={AuthorLen}, RawLen={RawLen}, Limit={Limit}",
            request.Title?.Length ?? 0,
            request.Author?.Length ?? 0,
            request.RawQuery?.Length ?? 0,
            request.Limit);

        var allResults = new Dictionary<string, OpenLibrarySearchDoc>();

        // Strategy 1: Title + Author (most specific)
        if (!string.IsNullOrWhiteSpace(request.Title) && !string.IsNullOrWhiteSpace(request.Author))
        {
            var titleAuthorQuery = new Dictionary<string, string>
            {
                { "title", request.Title },
                { "author", request.Author },
                { "limit", (request.Limit * 2).ToString() }
            };
            var results = await QueryOpenLibraryAsync(titleAuthorQuery, cancellationToken);
            MergeResults(allResults, results);
            _logger.LogDebug("Strategy Title+Author yielded {Count} items (accumulated {Total})", results.Count, allResults.Count);
        }

        // Strategy 2: Title only
        if (!string.IsNullOrWhiteSpace(request.Title) && allResults.Count < request.Limit)
        {
            var titleQuery = new Dictionary<string, string>
            {
                { "title", request.Title },
                { "limit", (request.Limit * 2).ToString() }
            };
            var results = await QueryOpenLibraryAsync(titleQuery, cancellationToken);
            MergeResults(allResults, results);
            _logger.LogDebug("Strategy Title-only yielded {Count} items (accumulated {Total})", results.Count, allResults.Count);
        }

        // Strategy 3: Author only
        if (!string.IsNullOrWhiteSpace(request.Author) && allResults.Count < request.Limit)
        {
            var authorQuery = new Dictionary<string, string>
            {
                { "author", request.Author },
                { "limit", (request.Limit * 2).ToString() }
            };
            var results = await QueryOpenLibraryAsync(authorQuery, cancellationToken);
            MergeResults(allResults, results);
            _logger.LogDebug("Strategy Author-only yielded {Count} items (accumulated {Total})", results.Count, allResults.Count);
        }

        // Strategy 4: Raw query (fallback)
        if (!string.IsNullOrWhiteSpace(request.RawQuery) && allResults.Count < request.Limit)
        {
            var rawQuery = new Dictionary<string, string>
            {
                { "q", request.RawQuery },
                { "limit", (request.Limit * 2).ToString() }
            };
            var results = await QueryOpenLibraryAsync(rawQuery, cancellationToken);
            MergeResults(allResults, results);
            _logger.LogDebug("Strategy Raw query yielded {Count} items (accumulated {Total})", results.Count, allResults.Count);
        }

        // Return top N results
        var final = allResults.Values
            .OrderByDescending(d => d.Edition_count ?? 0) // Prefer well-established works
            .Take(request.Limit)
            .ToList()
            .AsReadOnly();

        _logger.LogInformation("OpenLibrary search finished with {Count} unique results (returning {Returned})",
            allResults.Count, final.Count);
        return final;
    }

    /// <summary>
    /// Queries Open Library API with given parameters.
    /// </summary>
    private async Task<List<OpenLibrarySearchDoc>> QueryOpenLibraryAsync(
        Dictionary<string, string> queryParams,
        CancellationToken cancellationToken)
    {
        try
        {
            var queryString = string.Join("&",
                queryParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));
            var url = $"{OpenLibraryApiBase}?{queryString}";

            _logger.LogDebug("OpenLibrary GET {Url}", url);
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenLibrary request failed: {Status} for {Url} in {ElapsedMs:n0} ms", response.StatusCode, url, elapsedMs);
                return new List<OpenLibrarySearchDoc>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var docs = ParseSearchResults(json);
            _logger.LogInformation("OpenLibrary request OK: {Status} ({Count} docs) in {ElapsedMs:n0} ms",
                response.StatusCode, docs.Count, elapsedMs);
            return docs;
        }
        catch (Exception ex)
        {
            // Network errors, timeouts, etc. - return empty results
            _logger.LogError(ex, "OpenLibrary request error: {Message}", ex.Message);
            return new List<OpenLibrarySearchDoc>();
        }
    }

    /// <summary>
    /// Parses Open Library JSON response into domain models.
    /// </summary>
    private List<OpenLibrarySearchDoc> ParseSearchResults(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("docs", out var docsArray))
                return new List<OpenLibrarySearchDoc>();

            var results = new List<OpenLibrarySearchDoc>();

            foreach (var docElement in docsArray.EnumerateArray())
            {
                var doc_obj = new OpenLibrarySearchDoc();

                // Extract scalar properties
                if (docElement.TryGetProperty("key", out var keyProp))
                    doc_obj.Key = keyProp.GetString() ?? string.Empty;

                if (docElement.TryGetProperty("title", out var titleProp))
                    doc_obj.Title = titleProp.GetString() ?? string.Empty;

                if (docElement.TryGetProperty("author_name", out var authorNameProp))
                {
                    // author_name can be a string or an array of strings
                    if (authorNameProp.ValueKind == JsonValueKind.String)
                    {
                        doc_obj.Author_name = authorNameProp.GetString();
                    }
                    else if (authorNameProp.ValueKind == JsonValueKind.Array)
                    {
                        var first = authorNameProp.EnumerateArray()
                            .Select(a => a.ValueKind == JsonValueKind.String ? a.GetString() : null)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .FirstOrDefault();
                        doc_obj.Author_name = first;
                    }
                }

                if (docElement.TryGetProperty("author_names", out var authorNamesProp))
                {
                    if (authorNamesProp.ValueKind == JsonValueKind.Array)
                    {
                        doc_obj.Author_names = authorNamesProp.EnumerateArray()
                            .Select(a => a.GetString())
                            .Where(a => a != null)
                            .Cast<string>()
                            .ToArray();
                    }
                }

                if (docElement.TryGetProperty("first_publish_year", out var yearProp))
                {
                    if (yearProp.ValueKind == JsonValueKind.Number && yearProp.TryGetInt32(out var year))
                        doc_obj.First_publish_year = year;
                }

                if (docElement.TryGetProperty("edition_count", out var editionProp))
                {
                    if (editionProp.ValueKind == JsonValueKind.Number && editionProp.TryGetInt32(out var count))
                        doc_obj.Edition_count = count;
                }

                if (docElement.TryGetProperty("cover_id", out var coverProp))
                {
                    // cover_id may be numeric or string
                    if (coverProp.ValueKind == JsonValueKind.String)
                        doc_obj.Cover_id = coverProp.GetString();
                    else if (coverProp.ValueKind == JsonValueKind.Number && coverProp.TryGetInt32(out var cid))
                        doc_obj.Cover_id = cid.ToString();
                }

                // Store raw data
                doc_obj.RawData = JsonSerializer.Deserialize<Dictionary<string, object>>(docElement.GetRawText());

                results.Add(doc_obj);
            }

            return results;
        }
        catch (JsonException)
        {
            // Parsing failed - return empty results
            return new List<OpenLibrarySearchDoc>();
        }
    }

    /// <summary>
    /// Merges search results, deduplicating by Key.
    /// Keeps the result with higher edition count if duplicate found.
    /// </summary>
    private void MergeResults(
        Dictionary<string, OpenLibrarySearchDoc> target,
        List<OpenLibrarySearchDoc> toAdd)
    {
        foreach (var doc in toAdd)
        {
            if (string.IsNullOrEmpty(doc.Key))
                continue;

            if (target.TryGetValue(doc.Key, out var existing))
            {
                // Keep the one with higher edition count (better established)
                if ((doc.Edition_count ?? 0) > (existing.Edition_count ?? 0))
                    target[doc.Key] = doc;
            }
            else
            {
                target[doc.Key] = doc;
            }
        }
    }
}

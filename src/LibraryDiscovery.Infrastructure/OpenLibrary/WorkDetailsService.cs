using System.Collections.Concurrent;
using System.Text.Json;

namespace LibraryDiscovery.Infrastructure.OpenLibrary;

/// <summary>
/// Resolves primary authors for a work by calling Open Library detail endpoints:
///   GET /works/{workId}.json   → extracts authors[].author.key
///   GET /authors/{authorId}.json → extracts author name
///
/// Author names are cached in-memory for the lifetime of the service to avoid
/// redundant HTTP calls for shared authors (e.g. Tolkien across many works).
/// All failures are swallowed and return an empty array so the caller can fall
/// back to the search-API author fields.
/// </summary>
public class WorkDetailsService : IWorkDetailsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkDetailsService> _logger;
    private readonly ConcurrentDictionary<string, string> _authorNameCache = new();

    private const string OpenLibraryBase = "https://openlibrary.org";

    public WorkDetailsService(HttpClient httpClient, ILogger<WorkDetailsService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string[]> GetPrimaryAuthorsAsync(string workId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workId))
            return Array.Empty<string>();

        try
        {
            var workUrl = $"{OpenLibraryBase}/works/{workId}.json";
            _logger.LogDebug("Fetching work details: {Url}", workUrl);

            var response = await _httpClient.GetAsync(workUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Work details request failed: {Status} for workId={WorkId}", response.StatusCode, workId);
                return Array.Empty<string>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var authorKeys = ParseAuthorKeys(json);

            if (authorKeys.Length == 0)
                return Array.Empty<string>();

            var names = new List<string>(authorKeys.Length);
            foreach (var key in authorKeys)
            {
                var name = await ResolveAuthorNameAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }

            _logger.LogDebug("Resolved {Count} primary authors for workId={WorkId}", names.Count, workId);
            return names.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get primary authors for workId={WorkId}", workId);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Fetches an author's name by their OL key (e.g. /authors/OL26320A).
    /// Returns the cached value on repeat calls.
    /// </summary>
    private async Task<string?> ResolveAuthorNameAsync(string authorKey, CancellationToken cancellationToken)
    {
        if (_authorNameCache.TryGetValue(authorKey, out var cached))
            return cached;

        try
        {
            var url = $"{OpenLibraryBase}{authorKey}.json";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Author request failed: {Status} for {AuthorKey}", response.StatusCode, authorKey);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var name = ParseAuthorName(json);

            if (!string.IsNullOrEmpty(name))
                _authorNameCache[authorKey] = name;

            return name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch author {AuthorKey}", authorKey);
            return null;
        }
    }

    /// <summary>
    /// Parses author keys from a work JSON response.
    /// Expected shape: { "authors": [{ "author": { "key": "/authors/OL26320A" } }] }
    /// </summary>
    public static string[] ParseAuthorKeys(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("authors", out var authorsEl))
                return Array.Empty<string>();

            var keys = new List<string>();
            foreach (var entry in authorsEl.EnumerateArray())
            {
                if (entry.TryGetProperty("author", out var authorEl) &&
                    authorEl.TryGetProperty("key", out var keyEl))
                {
                    var key = keyEl.GetString();
                    if (!string.IsNullOrWhiteSpace(key))
                        keys.Add(key);
                }
            }
            return keys.ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Parses an author's name from an author JSON response.
    /// Prefers "name"; falls back to "personal_name".
    /// </summary>
    public static string? ParseAuthorName(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("name", out var nameProp))
                return nameProp.GetString();

            if (doc.RootElement.TryGetProperty("personal_name", out var personalProp))
                return personalProp.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }
}

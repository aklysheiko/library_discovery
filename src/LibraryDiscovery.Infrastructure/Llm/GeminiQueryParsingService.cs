using System.Text.Json;

namespace LibraryDiscovery.Infrastructure.Llm;

/// <summary>
/// Parses user queries using Google Gemini API with fallback to regex parser.
/// </summary>
public class GeminiQueryParsingService : IQueryParsingService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly IQueryParsingFallback _fallbackParser;
    private readonly ILogger<GeminiQueryParsingService> _logger;

    public GeminiQueryParsingService(
        HttpClient httpClient,
        IQueryParsingFallback fallbackParser,
        ILogger<GeminiQueryParsingService> logger,
        IConfiguration config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _fallbackParser = fallbackParser ?? throw new ArgumentNullException(nameof(fallbackParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = config["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        _model = config["GEMINI_MODEL"] ?? "models/gemini-2.5-flash";
        _baseUrl = config["GEMINI_BASE_URL"] ?? "https://generativelanguage.googleapis.com/v1beta";
    }

    /// <summary>
    /// Parses a messy query using Gemini API or falls back to regex parser on failure.
    /// </summary>
    public async Task<ParsedQuery> ParseAsync(string rawQuery, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return new ParsedQuery { RawQuery = rawQuery, ConfidenceNotes = "Empty query" };
        }

        // Try LLM first if API key available
        if (!string.IsNullOrEmpty(_apiKey))
        {
            try
            {
                _logger.LogInformation("Gemini parsing request: QueryLen={Len}", rawQuery.Length);
                return await ParseWithGeminiAsync(rawQuery, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log and fall through to fallback
                _logger.LogWarning(ex, "Gemini parsing failed: {Message}", ex.Message);
                var fallbackResult = _fallbackParser.Parse(rawQuery);
                fallbackResult.ConfidenceNotes = $"Gemini failed ({ex.Message}), used fallback parser";
                return fallbackResult;
            }
        }

        // No API key - use fallback immediately
        _logger.LogWarning("No Gemini API key configured, falling back to regex parser");
        var result = _fallbackParser.Parse(rawQuery);
        result.ConfidenceNotes = "No API key configured, used fallback parser";
        return result;
    }

    /// <summary>
    /// Calls Gemini API to extract query structure (titles, authors, keywords, year).
    /// </summary>
    private async Task<ParsedQuery> ParseWithGeminiAsync(string rawQuery, CancellationToken cancellationToken)
    {
        const string systemPrompt = @"Extract book search metadata from the user query. Return valid JSON with these fields:
{
  ""titles"": [""extracted title candidates""],
  ""authors"": [""author names""],
  ""keywords"": [""edition/genre hints like illustrated, deluxe, hardcover""],
  ""year"": null or 4-digit year,
  ""confidence"": ""high/medium/low""
}

Extract ONLY what is explicit or very strongly implied. Don't invent data.";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt },
                        new { text = $"Query: {rawQuery}" }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 500,
                responseMimeType = "application/json"
            }
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

        var url = $"{_baseUrl}/{_model}:generateContent?key={_apiKey}";
        _logger.LogDebug("Gemini POST {BaseUrl}/{Model} (promptLen={PromptLen})", _baseUrl, _model, jsonRequest.Length);
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var snippet = body.Length > 500 ? body.Substring(0, 500) + "…" : body;
            _logger.LogWarning("Gemini non-success {Status} in {Elapsed:n0} ms, bodySnippet={Snippet}", response.StatusCode, elapsedMs, snippet);
            throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var safeSnippet = responseJson.Length > 500 ? responseJson.Substring(0, 500) + "…" : responseJson;
        _logger.LogInformation("Gemini success in {Elapsed:n0} ms (responseSnippet={SnippetLen} chars)", elapsedMs, safeSnippet.Length);
        _logger.LogDebug("Gemini response snippet: {Snippet}", safeSnippet);
        return ParseGeminiResponse(rawQuery, responseJson);
    }

    /// <summary>
    /// Parses Gemini API response and extracts structured data.
    /// </summary>
    private ParsedQuery ParseGeminiResponse(string rawQuery, string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // Navigate: candidates[0].content.parts[0].text
            var candidates = root.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
                throw new JsonException("No candidates in response");

            var firstCandidate = candidates[0];
            var contentText = firstCandidate
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(contentText))
                throw new JsonException("Empty text in response");

            // Parse the JSON response from Gemini
            using var textDoc = JsonDocument.Parse(contentText);
            var textRoot = textDoc.RootElement;

            var titles = ExtractStringArray(textRoot, "titles");
            var authors = ExtractStringArray(textRoot, "authors");
            var keywords = ExtractStringArray(textRoot, "keywords");
            var year = ExtractYear(textRoot, "year");
            var confidence = textRoot.TryGetProperty("confidence", out var confProp)
                ? confProp.GetString()
                : "medium";

            return new ParsedQuery
            {
                RawQuery = rawQuery,
                TitleCandidates = titles,
                AuthorCandidates = authors,
                Keywords = keywords,
                YearHint = year,
                ConfidenceNotes = $"Gemini extraction ({confidence} confidence)"
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse Gemini response: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extracts a string array from JsonElement, handling null/missing gracefully.
    /// </summary>
    private IReadOnlyList<string> ExtractStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return Array.Empty<string>();

        if (prop.ValueKind == JsonValueKind.Null)
            return Array.Empty<string>();

        if (prop.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return prop.EnumerateArray()
            .Select(item => item.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Cast<string>()
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Extracts year as nullable int.
    /// </summary>
    private int? ExtractYear(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Null)
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var yearValue))
            return yearValue;

        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsedYear))
            return parsedYear;

        return null;
    }
}

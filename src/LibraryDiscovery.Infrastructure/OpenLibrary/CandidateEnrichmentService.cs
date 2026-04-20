namespace LibraryDiscovery.Infrastructure.OpenLibrary;

/// <summary>
/// Enriches Open Library search results with normalized data.
/// Deduplicates by canonical work key and normalizes titles/authors.
/// </summary>
public class CandidateEnrichmentService : ICandidateEnrichmentService
{
    private readonly IStringNormalizationService _normalizationService;
    private readonly IWorkDetailsService _workDetailsService;

    public CandidateEnrichmentService(
        IStringNormalizationService normalizationService,
        IWorkDetailsService workDetailsService)
    {
        _normalizationService = normalizationService ?? throw new ArgumentNullException(nameof(normalizationService));
        _workDetailsService = workDetailsService ?? throw new ArgumentNullException(nameof(workDetailsService));
    }

    /// <summary>
    /// Enriches search results with normalized data.
    /// Deduplicates by key and extracts author information.
    /// </summary>
    public async Task<IReadOnlyList<BookCandidate>> EnrichAsync(
        IReadOnlyList<OpenLibrarySearchDoc> docs,
        CancellationToken cancellationToken)
    {
        if (docs == null)
            throw new ArgumentNullException(nameof(docs));

        // Deduplicate by key (canonical work ID)
        var deduplicatedByKey = DeduplicateByKey(docs);

        var candidates = new List<BookCandidate>();

        foreach (var doc in deduplicatedByKey.Values)
        {
            try
            {
                var candidate = CreateCandidateFromDoc(doc);

                // Resolve true primary authors from the canonical work record.
                // The search API mixes contributors (illustrators, editors) into
                // author_name; /works/{id}.json.authors is the authoritative source.
                if (!string.IsNullOrEmpty(candidate.OpenLibraryWorkId))
                {
                    var primaryAuthors = await _workDetailsService
                        .GetPrimaryAuthorsAsync(candidate.OpenLibraryWorkId, cancellationToken);

                    if (primaryAuthors.Length > 0)
                    {
                        // Demote search-doc authors to contributors; they may include
                        // editors/illustrators that the work record does not list.
                        candidate.ContributorAuthors = candidate.PrimaryAuthors;
                        candidate.PrimaryAuthors = primaryAuthors;
                        candidate.NormalizedPrimaryAuthorSurnames = ExtractSurnames(primaryAuthors);
                    }
                }

                candidates.Add(candidate);
            }
            catch (Exception)
            {
                // Skip candidates that fail to process
            }
        }

        return candidates.AsReadOnly();
    }

    /// <summary>
    /// Deduplicates search results by Open Library key (canonical work ID).
    /// Keeps result with highest edition count if duplicates.
    /// Includes docs without keys.
    /// </summary>
    private Dictionary<string, OpenLibrarySearchDoc> DeduplicateByKey(
        IReadOnlyList<OpenLibrarySearchDoc> docs)
    {
        var deduplicated = new Dictionary<string, OpenLibrarySearchDoc>();
        var docsWithoutKey = new List<OpenLibrarySearchDoc>();

        foreach (var doc in docs)
        {
            if (string.IsNullOrEmpty(doc.Key))
            {
                // Keep docs without keys separate
                docsWithoutKey.Add(doc);
                continue;
            }

            if (deduplicated.TryGetValue(doc.Key, out var existing))
            {
                // Keep the one with higher edition count
                if ((doc.Edition_count ?? 0) > (existing.Edition_count ?? 0))
                    deduplicated[doc.Key] = doc;
            }
            else
            {
                deduplicated[doc.Key] = doc;
            }
        }

        // Add docs without keys at the end with unique generated keys
        foreach (var doc in docsWithoutKey)
        {
            deduplicated[$"_no_key_{deduplicated.Count}"] = doc;
        }

        return deduplicated;
    }

    /// <summary>
    /// Extracts work ID from Open Library key path.
    /// E.g., "/works/OL45883W" -> "OL45883W"
    /// </summary>
    private string ExtractWorkId(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var parts = key.Split('/');
        return parts.Length > 0 ? parts[^1] : string.Empty;
    }

    /// <summary>
    /// Creates BookCandidate from search doc.
    /// </summary>
    private BookCandidate CreateCandidateFromDoc(OpenLibrarySearchDoc doc)
    {
        var primaryAuthors = GetPrimaryAuthors(doc);
        var contributorAuthors = Array.Empty<string>();

        return new BookCandidate
        {
            OpenLibraryWorkId = ExtractWorkId(doc.Key),
            OpenLibraryKey = doc.Key,
            Title = doc.Title,
            NormalizedTitle = _normalizationService.Normalize(doc.Title),
            PrimaryAuthors = primaryAuthors,
            ContributorAuthors = contributorAuthors,
            NormalizedPrimaryAuthorSurnames = ExtractSurnames(primaryAuthors),
            FirstPublishYear = doc.First_publish_year ?? 0,
            EditionCount = doc.Edition_count ?? 0,
            CoverUrl = BuildCoverUrl(doc.Cover_id),
            RawData = doc.RawData ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Gets primary authors from search doc.
    /// Prioritizes author_names array over single author_name.
    /// </summary>
    private string[] GetPrimaryAuthors(OpenLibrarySearchDoc doc)
    {
        if (doc.Author_names != null && doc.Author_names.Length > 0)
            return doc.Author_names;

        if (!string.IsNullOrEmpty(doc.Author_name))
            return new[] { doc.Author_name };

        return Array.Empty<string>();
    }

    /// <summary>
    /// Extracts surnames from author names for matching.
    /// "J.R.R. Tolkien" -> "tolkien", multiple authors separated by space.
    /// </summary>
    private string ExtractSurnames(string[] authors)
    {
        if (authors.Length == 0)
            return string.Empty;

        var surnames = new List<string>();
        foreach (var author in authors)
        {
            var tokens = _normalizationService.ToTokens(author);
            if (tokens.Length > 0)
            {
                surnames.Add(tokens[^1]); // Last token is typically the surname
            }
        }

        return string.Join(" ", surnames);
    }

    /// <summary>
    /// Builds Open Library cover image URL.
    /// </summary>
    private string? BuildCoverUrl(string? coverId)
    {
        if (string.IsNullOrEmpty(coverId))
            return null;

        return $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
    }
}

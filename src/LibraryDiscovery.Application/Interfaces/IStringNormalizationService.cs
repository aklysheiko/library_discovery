namespace LibraryDiscovery.Application.Interfaces;

/// <summary>
/// Normalizes strings for consistent comparison and matching.
/// </summary>
public interface IStringNormalizationService
{
    /// <summary>
    /// Normalizes a string: lowercase, trim, punctuation removal, diacritics removal, collapse whitespace.
    /// </summary>
    /// <param name="input">The input string to normalize.</param>
    /// <returns>The normalized string.</returns>
    string Normalize(string input);

    /// <summary>
    /// Splits normalized input into tokens for token-based fuzzy matching.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>Array of normalized tokens.</returns>
    string[] ToTokens(string input);

    /// <summary>
    /// Normalizes input and removes common stop words (the, a, an).
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The normalized string with stop words removed.</returns>
    string NormalizeWithoutStopwords(string input);
}

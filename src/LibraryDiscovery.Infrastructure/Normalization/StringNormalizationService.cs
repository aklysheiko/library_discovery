using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LibraryDiscovery.Infrastructure.Normalization;

/// <summary>
/// Implementation of string normalization for consistent book matching.
/// </summary>
public class StringNormalizationService : IStringNormalizationService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an"
    };

    /// <summary>
    /// Normalizes a string by:
    /// 1. Trimming whitespace
    /// 2. Converting to lowercase
    /// 3. Removing diacritical marks
    /// 4. Removing punctuation
    /// 5. Collapsing multiple whitespaces to single space
    /// </summary>
    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Trim and lowercase
        var result = input.Trim().ToLowerInvariant();

        // Remove diacritical marks (e.g., é -> e, ä -> a)
        result = RemoveDiacritics(result);

        // Remove punctuation (keep only alphanumeric and spaces)
        result = Regex.Replace(result, @"[^\w\s]", " ", RegexOptions.CultureInvariant);

        // Collapse multiple spaces to single space
        result = Regex.Replace(result, @"\s+", " ");

        return result.Trim();
    }

    /// <summary>
    /// Splits a normalized string into tokens.
    /// First normalizes, then splits on whitespace.
    /// </summary>
    public string[] ToTokens(string input)
    {
        var normalized = Normalize(input);
        return normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Normalizes a string and removes common stop words ("the", "a", "an").
    /// </summary>
    public string NormalizeWithoutStopwords(string input)
    {
        var normalized = Normalize(input);
        var tokens = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var filtered = tokens.Where(t => !StopWords.Contains(t)).ToArray();
        return string.Join(" ", filtered);
    }

    /// <summary>
    /// Removes diacritical marks from a string.
    /// E.g., "Café" -> "Cafe", "Jürgen" -> "Jurgen"
    /// </summary>
    private static string RemoveDiacritics(string input)
    {
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}

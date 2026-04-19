namespace LibraryDiscovery.Application.DTOs;

/// <summary>
/// Request payload for the book match endpoint.
/// </summary>
public class BookMatchRequestDto
{
    /// <summary>
    /// The messy, plain-text book query from the user.
    /// Examples: "mark huckleberry", "tolkien hobbit illustrated deluxe 1937"
    /// </summary>
    public string Query { get; set; } = string.Empty;
}

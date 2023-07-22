namespace Elsa.Models;

/// <summary>
/// Represents a count response that offers a unified format for returning count of things from API endpoints.
/// </summary>
/// <param name="Count">The number of items found for a given query.</param>
public record CountResponse(long Count);
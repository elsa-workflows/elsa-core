namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a generic list response that offers a unified format for returning list of things from API endpoints.
/// </summary>
public record ListResponse<T>(ICollection<T> Items, long Count);
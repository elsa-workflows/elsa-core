namespace Elsa.Api.Client.Resources.IncidentStrategies.Models;

/// <summary>
/// Represents an incident strategy.
/// </summary>
/// <param name="TypeName">The .NET type name of the strategy.</param>
/// <param name="DisplayName">The display name of the strategy.</param>
/// <param name="Description">The description of the strategy.</param>
public record IncidentStrategyDescriptor(string TypeName, string DisplayName, string? Description);
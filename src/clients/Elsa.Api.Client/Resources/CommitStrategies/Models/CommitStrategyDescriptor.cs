namespace Elsa.Api.Client.Resources.CommitStrategies.Models;

/// <summary>
/// Represents a descriptor for a commit strategy, containing information such as its technical name,
/// display name, and description.
/// </summary>
public record CommitStrategyDescriptor(string Name, string DisplayName, string Description);
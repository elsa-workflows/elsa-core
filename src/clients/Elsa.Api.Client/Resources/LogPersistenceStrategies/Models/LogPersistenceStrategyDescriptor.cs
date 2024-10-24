using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.LogPersistenceStrategies;

/// <summary>
/// Represents a log persistence strategy.
/// </summary>
/// <param name="TypeName">The .NET type name of the log persistence strategy.</param>
/// <param name="DisplayName">The display name of the log persistence strategy.</param>
/// <param name="Description">The description of the log persistence strategy.</param>
[UsedImplicitly]
public record LogPersistenceStrategyDescriptor(string TypeName, string DisplayName, string? Description);
namespace Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;

/// <summary>
/// Represents a workflow activation strategy.
/// </summary>
/// <param name="TypeName">The .NET type name of the activation strategy.</param>
/// <param name="DisplayName">The display name of the activation strategy.</param>
/// <param name="Description">The description of the activation strategy.</param>
public record WorkflowActivationStrategyDescriptor(string TypeName, string DisplayName, string? Description);
namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a UI hint.
/// </summary>
/// <param name="Name">The name of the UI hint.</param>
/// <param name="DisplayName">The display name of the UI hint.</param>
/// <param name="Description">The description of the UI hint.</param>
public record UIHintDescriptor(string Name, string DisplayName, string? Description);
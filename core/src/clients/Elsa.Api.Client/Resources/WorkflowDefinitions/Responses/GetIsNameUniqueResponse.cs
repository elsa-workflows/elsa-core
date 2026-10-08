namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

/// <summary>
/// A response containing a value indicating whether a workflow definition name is unique.
/// </summary>
/// <param name="IsUnique"><c>true</c> if the name is unique, otherwise false.</param>
public record GetIsNameUniqueResponse(bool IsUnique);
namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// The response of a file import operation.
/// </summary>
/// <param name="Count">The number of workflows that were imported.</param>
public record ImportFilesResponse(int Count);
namespace Elsa.Api.Client.Resources.Tasks.Requests;

/// <summary>
/// Represents a request to report a task has been completed.
/// </summary>
/// <param name="TaskId">The Id of the task being reported as completed.</param>
/// <param name="Result">The result of the task being reported as completed.</param>
public record ReportTaskCompletedRequest(string TaskId, object? Result);
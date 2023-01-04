namespace Elsa.Samples.Webhooks.ExternalApp.Models;

public record RunTaskPayload(string TaskId, string TaskName, object? TaskParams);
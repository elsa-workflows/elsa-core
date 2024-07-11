namespace Elsa.Samples.AspNet.Webhooks.ExternalApp.Models;

public record RunTaskPayload(string TaskId, string TaskName, object? TaskParams);
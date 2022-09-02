using Elsa.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowRuntime
{
    Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowOptions options, CancellationToken cancellationToken = default);
    Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowOptions options, CancellationToken cancellationToken = default);
    Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string bookmarkName, object bookmarkPayload, TriggerWorkflowsOptions options, CancellationToken cancellationToken = default);
}

public record StartWorkflowOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default, VersionOptions VersionOptions = default);
public record ResumeWorkflowOptions(IDictionary<string, object>? Input = default);
public record StartWorkflowResult(string InstanceId);
public record ResumeWorkflowResult;
public record TriggerWorkflowsOptions(IDictionary<string, object>? Input = default);
public record TriggerWorkflowsResult;
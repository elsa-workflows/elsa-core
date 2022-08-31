using Elsa.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowRuntime
{
    Task StartWorkflowAsync(string definitionId, RunWorkflowOptions options, CancellationToken cancellationToken = default);
    Task ResumeWorkflowAsync(string definitionId, string instanceId, Bookmark bookmark, ResumeWorkflowOptions options, CancellationToken cancellationToken = default);
}

public record RunWorkflowOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default, VersionOptions VersionOptions = default);
public record ResumeWorkflowOptions(IDictionary<string, object>? Input = default);
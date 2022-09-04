using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Implementations;

public class WorkflowStateExporterService : IWorkflowStateExporterService
{
    private readonly IEnumerable<IWorkflowStateExporter> _workflowStateExporters;

    public WorkflowStateExporterService(IOptions<IWorkflowStateExporter> workflowStateExporters)
    {
        //_workflowStateExporters = workflowStateExporters;
    }

    public async Task ExportWorkflowStateAsync(WorkflowDefinition definition, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var tasks = _workflowStateExporters.Select(async exporter => await exporter.ExportAsync(definition, workflowState, cancellationToken));
        await Task.WhenAll(tasks);
    }
}
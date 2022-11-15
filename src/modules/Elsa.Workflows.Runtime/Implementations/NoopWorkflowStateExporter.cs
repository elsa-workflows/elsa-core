using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class NoopWorkflowStateExporter : IWorkflowStateExporter
{
    public ValueTask ExportAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
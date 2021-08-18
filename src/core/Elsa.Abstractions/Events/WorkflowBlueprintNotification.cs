using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    /// <summary>
    /// Common base for workflow blueprint related events.
    /// </summary>
    public abstract class WorkflowBlueprintNotification : INotification
    {
        protected WorkflowBlueprintNotification(IWorkflowBlueprint workflowBlueprint) => WorkflowBlueprint = workflowBlueprint;
        public IWorkflowBlueprint WorkflowBlueprint { get; }
    }
}
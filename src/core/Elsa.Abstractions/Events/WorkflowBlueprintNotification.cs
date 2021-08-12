using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    /// <summary>
    /// Common base for workflow blueprint related events.
    /// </summary>
    public abstract class WorkflowBlueprintNotification : INotification
    {
        protected WorkflowBlueprintNotification(WorkflowBlueprint workflowBlueprint)
        {
            WorkflowBlueprint = workflowBlueprint;
        }

        public WorkflowBlueprint WorkflowBlueprint { get; }
    }
}
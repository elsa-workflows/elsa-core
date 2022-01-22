using System.Collections.Generic;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public record TriggerIndexingFinished(IWorkflowBlueprint WorkflowBlueprint, IReadOnlyCollection<WorkflowTrigger> Triggers) : INotification;
}
using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public record TriggerIndexingFinished(string WorkflowDefinitionId, IReadOnlyCollection<Trigger> Triggers) : INotification;
}
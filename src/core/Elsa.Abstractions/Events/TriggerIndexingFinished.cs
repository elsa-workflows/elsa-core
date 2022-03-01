using System.Collections.Generic;
using Elsa.Abstractions.Multitenancy;
using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public record TriggerIndexingFinished(string WorkflowDefinitionId, IReadOnlyCollection<Trigger> Triggers, ITenant Tenant) : INotification;
}
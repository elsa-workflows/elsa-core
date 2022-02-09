using System.Collections.Generic;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;
using MediatR;

namespace Elsa.Events
{
    public record TriggerIndexingFinished(string WorkflowDefinitionId, IReadOnlyCollection<Trigger> Triggers, Tenant Tenant) : INotification;
}
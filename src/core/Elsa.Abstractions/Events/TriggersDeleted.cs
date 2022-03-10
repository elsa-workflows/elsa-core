using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Events;

public record TriggersDeleted(string WorkflowDefinitionId, IReadOnlyCollection<Trigger> Triggers) : INotification;
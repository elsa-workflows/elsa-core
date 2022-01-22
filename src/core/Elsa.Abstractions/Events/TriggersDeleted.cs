using System.Collections.Generic;
using Elsa.Services;
using MediatR;

namespace Elsa.Events;

public record TriggersDeleted(IReadOnlyCollection<WorkflowTrigger> Triggers) : INotification;
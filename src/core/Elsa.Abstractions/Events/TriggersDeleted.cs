using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events;

public record TriggersDeleted(IReadOnlyCollection<WorkflowTrigger> Triggers) : INotification;
using System.Collections.Generic;
using Elsa.Activities.Signaling.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Activities.Http.Events
{
    public record HttpTriggeredSignal(SignalModel SignalModel, ICollection<CollectedWorkflow> AffectedWorkflows) : INotification;
}
using MediatR;

namespace Elsa.Events
{
    public record MultitenantTriggerIndexingFinished : INotification
    {
    }
}
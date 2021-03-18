using Elsa.ActivityProviders;
using Elsa.Metadata;
using MediatR;

namespace Elsa.Events
{
    public record DescribingActivityType(ActivityType ActivityType, ActivityDescriptor ActivityDescriptor) : INotification;
}
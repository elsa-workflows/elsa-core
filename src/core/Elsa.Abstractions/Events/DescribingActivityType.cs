using Elsa.Metadata;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public record DescribingActivityType(ActivityType ActivityType, ActivityDescriptor ActivityDescriptor) : INotification;
}
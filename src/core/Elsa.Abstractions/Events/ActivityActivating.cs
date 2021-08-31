using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class ActivityActivating : INotification
    {
        public ActivityExecutionContext ActivityExecutionContext { get; }

        public ActivityActivating(ActivityExecutionContext activityExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
        }
    }
}
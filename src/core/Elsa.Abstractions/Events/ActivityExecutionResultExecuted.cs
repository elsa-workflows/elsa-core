using Elsa.ActivityResults;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class ActivityExecutionResultExecuted : INotification
    {
        public ActivityExecutionResultExecuted(IActivityExecutionResult result, ActivityExecutionContext activityExecutionContext)
        {
            Result = result;
            ActivityExecutionContext = activityExecutionContext;
        }

        public IActivityExecutionResult Result { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}
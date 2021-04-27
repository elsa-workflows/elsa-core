using Elsa.ActivityResults;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class ActivityExecutionResultExecuting : INotification
    {
        public ActivityExecutionResultExecuting(IActivityExecutionResult result, ActivityExecutionContext activityExecutionContext)
        {
            Result = result;
            ActivityExecutionContext = activityExecutionContext;
        }

        public IActivityExecutionResult Result { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}
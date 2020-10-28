using Elsa.Services.Models;

namespace Elsa
{
    public static class ActivityExecutionContextExtensions
    {
        public static T GetOutputFrom<T>(this ActivityExecutionContext activityExecutionContext, string activityName) => (T)activityExecutionContext.GetOutputFrom(activityName)!;
    }
}
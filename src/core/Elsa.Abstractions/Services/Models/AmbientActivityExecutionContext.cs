using System;
using System.Threading;

namespace Elsa.Services.Models
{
    public static class AmbientActivityExecutionContext
    {
        private static readonly AsyncLocal<ActivityExecutionContext?> ActivityExecutionContext = new();

        public static ActivityExecutionContext? Current
        {
            get => ActivityExecutionContext.Value;
            private set => ActivityExecutionContext.Value = value;
        }

        public static ActivityExecutionContextScope EnterScope(ActivityExecutionContext activityExecutionContext)
        {
            Current = activityExecutionContext;
            return new ActivityExecutionContextScope(() => Current = null);
        }
    }

    public record ActivityExecutionContextScope(Action Reset) : IDisposable
    {
        public void Dispose()
        {
            Reset();
        }
    }
}
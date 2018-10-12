using System;
using Flowsharp.Activities;
using Flowsharp.ActivityResults;
using Flowsharp.Models;

namespace Flowsharp.ActivityProviders
{
    /// <summary>
    /// Provides activities based on <see cref="IActivity"/> implementations that have been registered with the service container.
    /// </summary>
    public class WriteLine : Activity
    {
        protected override ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            Console.WriteLine("Hello World!");
            return Outcomes("Done");
        }
    }
}

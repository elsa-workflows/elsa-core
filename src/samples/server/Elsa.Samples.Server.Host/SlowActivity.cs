using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Services.Models;
using Activity = Elsa.Services.Activity;

namespace Elsa.Samples.Server.Host
{
    public class SlowActivity : Activity
    {
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            Trace.WriteLine($"Starting my slow task.. {context.WorkflowInstance.Id}.{context.ActivityId}.  {DateTime.Now.TimeOfDay}");
            
            await Task.Delay(10000);

            Trace.WriteLine($"Phew, I finished!  {context.WorkflowInstance.Id}. {context.ActivityId}.  {DateTime.Now.TimeOfDay}");

            return Done();
        }
    }
}
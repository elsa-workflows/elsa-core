using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.ActivityProviders;
using Flowsharp.Models;
using Flowsharp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowsharp.Samples.Console
{
    class Program
    {
        static async Task Main()
        {
            var workflowType = new WorkflowType
            {
                Activities = new[]
                {
                    new ActivityType("1", "WriteLine")
                },
                Transitions = new Transition[0]
            };

            var typedActivityProvider = new TypedActivityProvider(() => new IActivity[] { new WriteLine() } );
            var activityLibrary = new ActivityLibrary(new[]{ typedActivityProvider });
            var dictionary = await activityLibrary.GetActivityDescriptorDictionaryAsync(CancellationToken.None);
            var workflowContext = new WorkflowExecutionContext(dictionary, workflowType, WorkflowStatus.Idle);
            var invoker = new WorkflowInvoker(new Logger<WorkflowInvoker>(new NullLoggerFactory()));
            
            await invoker.InvokeAsync(workflowContext, "1", CancellationToken.None);
        }
    }
}

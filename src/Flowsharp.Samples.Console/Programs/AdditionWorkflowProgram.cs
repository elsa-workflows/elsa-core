using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Samples.Console.Workflows;
using Flowsharp.Serialization;
using Flowsharp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowsharp.Samples.Console.Programs
{
    public class AdditionWorkflowProgram
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var invoker = new WorkflowInvoker(new Logger<WorkflowInvoker>(new NullLoggerFactory()));
            var workflow = new AdditionWorkflow();
            var workflowContext = await invoker.InvokeAsync(workflow, null, cancellationToken);
            var serializer = new JsonWorkflowSerializer();
            var json = await serializer.SerializeAsync(workflow, cancellationToken);

            System.Console.WriteLine(json);
        }
    }
}
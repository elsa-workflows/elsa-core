using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Expressions;
using Flowsharp.Handlers;
using Flowsharp.Samples.Console.Activities;
using Flowsharp.Samples.Console.Handlers;
using Flowsharp.Samples.Console.Workflows;
using Flowsharp.Serialization;
using Flowsharp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowsharp.Samples.Console.Programs
{
    public class AdditionWorkflowProgram
    {
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowSerializer serializer;

        public AdditionWorkflowProgram(IWorkflowInvoker workflowInvoker, IWorkflowSerializer serializer)
        {
            this.workflowInvoker = workflowInvoker;
            this.serializer = serializer;
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var workflow = new AdditionWorkflow();
            var workflowContext = await workflowInvoker.InvokeAsync(workflow, null, cancellationToken);
            var json = await serializer.SerializeAsync(workflow, cancellationToken);

            System.Console.WriteLine(json);
        }
    }
}
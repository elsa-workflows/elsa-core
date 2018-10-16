using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Samples.Console.Workflows;
using Flowsharp.Serialization;

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
            var workflowContext = await workflowInvoker.InvokeAsync(workflow, null, Variables.Empty, cancellationToken);
            var json = serializer.Serialize(workflow);

            System.Console.WriteLine(json);
        }
    }
}
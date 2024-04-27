using Elsa.Abstractions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;

namespace Elsa.Server.Web.Endpoints.DynamicWorkflows.Post;

public class Post(IWorkflowRegistry workflowRegistry, IWorkflowInvoker workflowInvoker) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/dynamic-workflows");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var workflow = new Workflow
        {
            Identity = new WorkflowIdentity("DynamicWorkflow1", 1, "DynamicWorkflow1:v1"),
            Root = new Sequence
            {
                Activities =
                {
                    new WriteLine("Step 1"),
                    new WriteLine("Step 2"),
                    new WriteLine("Step 3")
                }
            }
        };

        await workflowInvoker.InvokeAsync(workflow, new InvokeWorkflowOptions(), ct);
    }
}
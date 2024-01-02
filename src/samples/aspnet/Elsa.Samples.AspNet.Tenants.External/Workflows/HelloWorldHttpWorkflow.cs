using Elsa.Http;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Samples.AspNet.Tenants;

public class HelloWorldHttpWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new HttpEndpoint
                {
                    Path = new("/hello-world"),
                    CanStartWorkflow = true
                },
                new WriteHttpResponse
                {
                    Content = new("Hello world of HTTP workflows!")
                }
            }
        };
    }
}
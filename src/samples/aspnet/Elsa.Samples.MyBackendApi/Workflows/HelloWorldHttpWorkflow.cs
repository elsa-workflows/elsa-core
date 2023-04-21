using Elsa.Http;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.MyBackendApi.Workflows;

public class HelloWorldHttpWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        (
            new HttpEndpoint
            {
                Path = new("/hello-world"),
                CanStartWorkflow = true
            },
            new WriteHttpResponse
            {
                Content = new("Hello world of HTTP workflows!")
            }
        );
    }
}
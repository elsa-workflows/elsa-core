using Elsa.Http;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.HttpWorkflows.Workflows;

public class SimpleHttpApiWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("simple-http-api/start"),
                    SupportedMethods = new([HttpMethods.Get]),
                    CanStartWorkflow = true
                },
                new HttpEndpoint
                {
                    Path = new("simple-http-api/resume"),
                    SupportedMethods = new([HttpMethods.Get])
                }
            ]
        };
    }
}
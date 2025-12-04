using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.ConcurrentTriggerIndexing;

public class HttpWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("my-workflow"),
                    SupportedMethods = new([HttpMethods.Get]),
                    CanStartWorkflow = true
                }
            ]
        };
    }
}
using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class RequestSizeLimitWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var parsedContentVariable = builder.WithVariable<object>();

        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("test/request-size-limit"),
                    SupportedMethods = new([HttpMethods.Post]),
                    RequestSizeLimit = new(8),
                    CanStartWorkflow = true,
                    ParsedContent = new(parsedContentVariable)
                },
                new WriteHttpResponse
                {
                    Content = new(context => parsedContentVariable.Get(context)?.ToString() ?? "No content received"),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}

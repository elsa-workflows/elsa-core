using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class JsonContentWorkflow : WorkflowBase
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
                    Path = new("test/json-content"),
                    SupportedMethods = new([HttpMethods.Post]),
                    CanStartWorkflow = true,
                    ParsedContent = new(parsedContentVariable)
                },
                new WriteHttpResponse
                {
                    Content = new(context => 
                    {
                        var content = parsedContentVariable.Get(context);
                        return content != null 
                            ? JsonSerializer.Serialize(content)
                            : "No content received";
                    }),
                    ContentType = new("application/json"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}


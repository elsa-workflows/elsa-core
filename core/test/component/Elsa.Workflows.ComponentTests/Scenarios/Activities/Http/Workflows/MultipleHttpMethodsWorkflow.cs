using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class MultipleHttpMethodsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        
        var requestMethodVariable = builder.WithVariable<string>();
        
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("test/multi-method"),
                    SupportedMethods = new([HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete]),
                    CanStartWorkflow = true
                },
                new SetVariable
                {
                    Variable = requestMethodVariable,
                    Value = new(context => context.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.Method)
                },
                new WriteHttpResponse
                {
                    Content = new(context => $"Method: {requestMethodVariable.Get(context)}"),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}

using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class FormDataWorkflow : WorkflowBase
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
                    Path = new("test/form-data"),
                    SupportedMethods = new([HttpMethods.Post]),
                    CanStartWorkflow = true,
                    ParsedContent = new(parsedContentVariable)
                },
                new WriteHttpResponse
                {
                    Content = new(context => 
                    {
                        var content = parsedContentVariable.Get(context);
                        if (content is IDictionary<string, object> formData)
                        {
                            var name = formData.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? "unknown" : "unknown";
                            var email = formData.TryGetValue("email", out var emailObj) ? emailObj?.ToString() ?? "unknown" : "unknown";
                            return $"Name: {name}, Email: {email}";
                        }
                        return "No form data received";
                    }),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}


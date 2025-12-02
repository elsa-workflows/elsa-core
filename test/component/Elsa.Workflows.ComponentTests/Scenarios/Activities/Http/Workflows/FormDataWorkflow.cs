using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class FormDataWorkflow : WorkflowBase
{
    private static readonly string DefinitionId = Guid.NewGuid().ToString();

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
                            string name;
                            if (formData.TryGetValue("name", out var nameObj) && nameObj != null)
                                name = nameObj.ToString();
                            else
                                name = "unknown";
                            string email;
                            if (formData.TryGetValue("email", out var emailObj) && emailObj != null)
                                email = emailObj.ToString();
                            else
                                email = "unknown";
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


using System.Net.Mime;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.AspNet.HttpEndpoints.Workflows;

/// <summary>
/// A workflow that acts as an HTTP endpoint to which a client can POST a JSON payload.
/// </summary>
public class SubmissionWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        // The payload will be deserialized into a dynamic object. The JSON might look like this:
        // {
        //   "name": "John Doe",
        //   "email": "john.doe@localhost"
        // }
        var payloadVariable = builder.WithVariable<dynamic>().WithMemoryStorage();

        builder.Root = new Sequence
        {
            Activities =
            {
                new HttpEndpoint
                {
                    Path = new("submit"),
                    SupportedMethods = new(new[]{HttpMethods.Post}),
                    CanStartWorkflow = true,
                    ParsedContent = new(payloadVariable)
                },
                new WriteHttpResponse
                {
                    ContentType = new(MediaTypeNames.Application.Json),
                    Content = new(context =>
                    {
                        var payload = payloadVariable.Get(context)!;
                        var name = payload.name;
                        var email = payload.email;
                        
                        return new
                        {
                            Message = $"Dear {name}, thank you for submitting your information! We'll send you an email at {email} shortly.",
                        };
                    }),
                }
            }
        };
    }
}
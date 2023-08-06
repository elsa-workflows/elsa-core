using System.Net.Mime;
using Elsa.Http;
using Elsa.Samples.AspNet.HttpEndpoints.Activities;
using Elsa.Samples.AspNet.HttpEndpoints.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.AspNet.HttpEndpoints.Workflows;

/// <summary>
/// A workflow that acts as an HTTP endpoint to which a client can POST a JSON payload which gts parsed into a strongly typed model.
/// </summary>
public class TypedSubmissionWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        // The payload will be deserialized into a Customer object. The JSON must look like this:
        // {
        //   "name": "John Doe",
        //   "email": "john.doe@localhost"
        // }
        var customerVariable = builder.WithVariable<CustomerDto>();
        
        var mappedCustomerVariable = builder.WithVariable<Customer>();

        builder.Root = new Sequence
        {
            Activities =
            {
                // Define an HTTP endpoint.
                new HttpEndpoint
                {
                    Path = new("customers"),
                    SupportedMethods = new(new[]{HttpMethods.Post}),
                    CanStartWorkflow = true,
                    ParsedContent = new(customerVariable)
                },
                
                // Map the DTO to a domain model.
                new MapTo<CustomerDto, Customer>
                {
                    Source = new(customerVariable),
                    Result = new(mappedCustomerVariable)
                },
                
                // Write a response.
                new WriteHttpResponse
                {
                    ContentType = new(MediaTypeNames.Application.Json),
                    Content = new(context =>
                    {
                        var customer = mappedCustomerVariable.Get(context)!;
                        var name = customer.Name;
                        var email = customer.Email;
                        
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
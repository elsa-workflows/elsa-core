using System.Dynamic;
using Elsa.Http;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.OutboundHttpRequests.Workflows;

/// <summary>
/// A workflow that retrieves a list of users from an API endpoint and prints out the names to the console output.
/// </summary>
public class GetUsersWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var responseVariable = builder.WithVariable<ExpandoObject>();
        var currentUserVariable = builder.WithVariable<ExpandoObject>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new SendHttpRequest
                {
                    Url = new(new Uri("https://reqres.in/api/users")),
                    Method = new(HttpMethods.Get),
                    ParsedContent = new(responseVariable)
                },
                new ForEach<ExpandoObject>(context =>
                {
                    var response = (dynamic)responseVariable.Get(context)!;
                    return (ICollection<ExpandoObject>)response.data;
                })
                {
                    CurrentValue = new(currentUserVariable),
                    Body = new WriteLine(context =>
                    {
                        var currentUser = (dynamic)currentUserVariable.Get(context)!;
                        return $"{currentUser.first_name} {currentUser.last_name}";
                    })
                }
            }
        };
    }
}
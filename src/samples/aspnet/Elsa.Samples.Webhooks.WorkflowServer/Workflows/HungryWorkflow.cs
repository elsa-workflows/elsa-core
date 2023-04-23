using System.Collections.Generic;
using Elsa.Http;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.Webhooks.WorkflowServer.Workflows;

public class HungryWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var deliveredFood = builder.WithVariable<string>();
        
        builder.Root = new Sequence
        {
            Activities =
            {
                new HttpEndpoint
                {
                    Path = new("/hungry"),
                    SupportedMethods = new (new[]{HttpMethods.Post}),
                    CanStartWorkflow = true
                },
                new WriteLine("Hunger detected!"),
                new RunTask("OrderFood")
                {
                    Payload = new(new Dictionary<string, object>() { ["Food"] = "Pizza" }),
                    Result = new Output<object>(deliveredFood)
                },
                new WriteLine(context => $"Eating the {deliveredFood.Get(context)}"),
                new WriteLine("Hunger satisfied!")
            }
        };
    }
}
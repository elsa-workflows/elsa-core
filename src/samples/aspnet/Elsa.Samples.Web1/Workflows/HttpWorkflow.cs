using System.Collections.Generic;
using System.Net;
using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;
using Elsa.Modules.Http;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

using HttpMethods = Microsoft.AspNetCore.Http.HttpMethods;

public class HttpWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        // Setup the workflow.
        workflow
            .WithDefinitionId(nameof(HttpWorkflow))
            .WithVersion(1)
            
            // Workflows have one starting node, aka its "root".
            .WithRoot(new Sequence
            {
                Activities =
                {
                    new HttpEndpoint
                    {
                        CanStartWorkflow = true,
                        Path = new Input<string>("/hello-world"),
                        SupportedMethods = new Input<ICollection<string>>(new[] { HttpMethods.Get })
                    },
                    new If
                    {
                        Condition = new Input<bool>(true),
                        Then = new Sequence(
                            new WriteLine("It's true!"),
                            new HttpEndpoint
                            {
                                Path = new Input<string>("/hello-world/true"),
                                SupportedMethods = new Input<ICollection<string>>(new[] { HttpMethods.Post })
                            },
                            new WriteLine("Let's continue")
                        ),
                        Else = new WriteLine("It's not true!")
                    },
                    new WriteHttpResponse
                    {
                        StatusCode = new Input<HttpStatusCode>(HttpStatusCode.OK),
                        Content = new Input<string?>("Hello World!")
                    }
                }
            });
    }
}
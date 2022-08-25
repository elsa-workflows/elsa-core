using System.Collections.Generic;
using System.Net;
using Elsa.Http;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

using HttpMethods = Microsoft.AspNetCore.Http.HttpMethods;

public class HttpWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
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
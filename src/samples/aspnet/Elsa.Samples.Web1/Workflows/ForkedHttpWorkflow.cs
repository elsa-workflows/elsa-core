using System.Collections.Generic;
using System.Net;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Runtime.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.Web1.Workflows;

public class ForkedHttpWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        // Add triggers.
        workflow.AddTrigger(new HttpTrigger
        {
            Path = new Input<string>("/fork"),
            SupportedMethods = new Input<ICollection<string>>(new[] { HttpMethods.Get })
        });

        // Setup workflow graph.
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new Fork
                {
                    JoinMode = new Input<JoinMode>(JoinMode.WaitAll),
                    Branches =
                    {
                        new Sequence
                        {
                            Activities =
                            {
                                new HttpTrigger { Path = new Input<string>("/fork/branch-1"), },
                                new WriteLine("Branch 1 continues!")
                            }
                        },
                        new Sequence
                        {
                            Activities =
                            {
                                new HttpTrigger { Path = new Input<string>("/fork/branch-2"), },
                                new WriteLine("Branch 2 continues!")
                            }
                        }
                    },
                },
                new WriteHttpResponse
                {
                    StatusCode = new Input<HttpStatusCode>(HttpStatusCode.OK),
                    Content = new Input<string?>("Done!")
                }
            }
        });
    }
}
using System.Dynamic;
using System.Net;
using System.Net.Mime;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Samples.AspNet.DocumentApproval
{
    /// <summary>
    /// A workflow that gives Jack and Lucy a chance to review a submitted document.
    /// </summary>
    public class DocumentApprovalWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            var documentVariable = builder.WithVariable<ExpandoObject>();
            var approvedVariable = builder.WithVariable<bool>();

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HttpEndpoint
                    {
                        Path = new("/documents"),
                        SupportedMethods = new(new[] { HttpMethods.Post }),
                        ParsedContent = new(documentVariable),
                        CanStartWorkflow = true
                    },
                    new WriteLine(context => $"Document received from {documentVariable.Get<dynamic>(context)!.Author.Name}."),
                    new WriteHttpResponse
                    {
                        Content = new("<h1>Request for Approval Sent</h1><p>Your document has been received and will be reviewed shortly.</p>"),
                        ContentType = new(MediaTypeNames.Text.Html),
                        StatusCode = new(HttpStatusCode.OK),
                        ResponseHeaders = new(new HttpResponseHeaders { ["X-Powered-By"] = new[] { "Elsa 3.0" } })
                    },
                    new Fork
                    {
                        JoinMode = ForkJoinMode.WaitAll,
                        Branches =
                        {
                            // Jack
                            new Sequence
                            {
                                Activities =
                                {
                                    new WriteLine(context => $"Jack approve url: \n {GenerateSignalUrl(context, "Approve:Jack")}"),
                                    new WriteLine(context => $"Jack reject url: \n {GenerateSignalUrl(context, "Reject:Jack")}"),
                                    new Fork
                                    {
                                        JoinMode = ForkJoinMode.WaitAny,
                                        Branches =
                                        {
                                            // Approve
                                            new Sequence
                                            {
                                                Activities =
                                                {
                                                    new Event("Approve:Jack"),
                                                    new SetVariable
                                                    {
                                                        Variable = approvedVariable,
                                                        Value = new(true)
                                                    },
                                                    new WriteHttpResponse
                                                    {
                                                        Content = new("Thanks for the approval, Jack!"),
                                                    }
                                                }
                                            },

                                            // Reject
                                            new Sequence
                                            {
                                                Activities =
                                                {
                                                    new Event("Reject:Jack"),
                                                    new SetVariable
                                                    {
                                                        Variable = approvedVariable,
                                                        Value = new(false)
                                                    },
                                                    new WriteHttpResponse
                                                    {
                                                        Content = new("Sorry to hear that, Jack!"),
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },

                            // Lucy
                            new Sequence
                            {
                                Activities =
                                {
                                    new WriteLine(context => $"Lucy approve url: \n {GenerateSignalUrl(context, "Approve:Lucy")}"),
                                    new WriteLine(context => $"Lucy reject url: \n {GenerateSignalUrl(context, "Reject:Lucy")}"),
                                    new Fork
                                    {
                                        JoinMode = ForkJoinMode.WaitAny,
                                        Branches =
                                        {
                                            // Approve
                                            new Sequence
                                            {
                                                Activities =
                                                {
                                                    new Event("Approve:Lucy"),
                                                    new SetVariable
                                                    {
                                                        Variable = approvedVariable,
                                                        Value = new(true)
                                                    },
                                                    new WriteHttpResponse
                                                    {
                                                        Content = new("Thanks for the approval, Lucy!"),
                                                    }
                                                }
                                            },

                                            // Reject
                                            new Sequence
                                            {
                                                Activities =
                                                {
                                                    new Event("Reject:Lucy"),
                                                    new SetVariable
                                                    {
                                                        Variable = approvedVariable,
                                                        Value = new(false)
                                                    },
                                                    new WriteHttpResponse
                                                    {
                                                        Content = new("Sorry to hear that, Lucy!"),
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new WriteLine(context => $"Approved: {approvedVariable.Get<bool>(context)}"),
                    new If(context => approvedVariable.Get<bool>(context))
                    {
                        Then = new WriteLine(context => $"Document ${documentVariable.Get<dynamic>(context)!.Id} approved!"),
                        Else = new WriteLine(context => $"Document ${documentVariable.Get<dynamic>(context)!.Id} rejected!")
                    }
                }
            };
        }

        private string GenerateSignalUrl(ExpressionExecutionContext context, string signalName)
        {
            return context.GenerateEventTriggerUrl(signalName);
        }
    }
}
using System.Net;
using System.Net.Http;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Samples.DocumentApproval
{
    /// <summary>
    /// A workflow that gives Jack and Lucy a chance to review a submitted document.
    /// </summary>
    public class DocumentApprovalWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .HttpEndpoint(activity => activity
                    .WithMethod(HttpMethod.Post.Method)
                    .WithPath("/documents")
                    .WithReadContent())
                .SetVariable("Document", context => context.GetInput<HttpRequestModel>()!.Body)
                .WriteLine(context => $"Document received from {context.GetVariable<dynamic>("Document")!.Author.Name}")
                .WriteHttpResponse(activity => activity
                    .WithContent(context => "<h1>Request for Approval Sent</h1><p>Your document has been received and will be reviewed shortly.</p>")
                    .WithContentType("text/html")
                    .WithStatusCode(HttpStatusCode.OK).WithResponseHeaders(new HttpResponseHeaders { ["X-Powered-By"] = "Elsa Workflows" })
                )
                .Then<Fork>(fork1 => fork1.WithBranches("Jack", "Lucy"), fork1 =>
                    {
                        fork1
                            .When("Jack")
                            .WriteLine(context => $"Jack approve url: \n {context.GenerateSignalUrl("Approve:Jack")}")
                            .Then<Fork>(fork2 => fork2.WithBranches("Approve", "Reject"), fork2 =>
                            {
                                fork2
                                    .When("Approve")
                                    .SignalReceived("Approve:Jack")
                                    .SetVariable("Approved", context => context.SetVariable<int>("Approved", approved => approved == 0 ? 1 : approved))
                                    .ThenNamed("JoinJack");

                                fork2
                                    .When("Reject")
                                    .SignalReceived("Reject:Jack")
                                    .SetVariable<int>("Approved", 2)
                                    .ThenNamed("JoinJack");
                            }).WithName("ForkJack")
                            .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("JoinJack")
                            .ThenNamed("JoinJackLucy");

                        fork1.When("Lucy")
                            .WriteLine(context => $"Lucy approve url: \n {context.GenerateSignalUrl("Approve:Lucy")}")
                            .Then<Fork>(fork2 => fork2.WithBranches("Approve", "Reject"),
                                fork2 =>
                                {
                                    fork2
                                        .When("Approve")
                                        .SignalReceived("Approve:Lucy")
                                        .SetVariable("Approved", context => context.SetVariable<int>("Approved", approved => approved == 0 ? 1 : approved))
                                        .ThenNamed("JoinLucy");

                                    fork2
                                        .When("Reject")
                                        .SignalReceived("Reject:Lucy")
                                        .SetVariable<int>("Approved", 2)
                                        .ThenNamed("JoinLucy");
                                }).WithName("ForkLucy")
                            .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("JoinLucy")
                            .ThenNamed("JoinJackLucy");
                    }
                )
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAll)).WithName("JoinJackLucy")
                .WriteLine(context => $"Approved: {context.GetVariable<int>("Approved")}").WithName("AfterJoinJackLucy")
                .If(context => context.GetVariable<int>("Approved") == 1, @if =>
                {
                    @if
                        .When(OutcomeNames.True)
                        .WriteLine(context => $"Document ${context.GetVariable<dynamic>("Document")!.Id} approved!`");

                    @if
                        .When(OutcomeNames.False)
                        .WriteLine(context => $"Document ${context.GetVariable<dynamic>("Document")!.Id} rejected!`");
                });
        }
    }
}
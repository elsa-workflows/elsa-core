using System.Net;
using System.Net.Http;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.NestedForks.Workflows
{
    public class NestedForksWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Sample Workflow")
                .HttpEndpoint(activity => activity
                    .WithPath("/trigger")
                    .WithMethod(HttpMethod.Get.Method))
                .WriteHttpResponse(http => http
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContent(context => context.WorkflowInstance.Id))
                .Then<Fork>(activity => activity.WithBranches("ChildOne", "ChildTwo"), fork =>
                {
                    fork
                        .When("ChildOne")
                        .Then<Fork>(activity => activity.WithBranches("ChildOneSuccess", "ChildOneFailed"), childOneFork =>
                        {
                            childOneFork.When("ChildOneSuccess")
                                .SignalReceived("ChildOneSuccess")
                                .WriteHttpResponse(http => http
                                    .WithStatusCode(HttpStatusCode.OK)
                                    .WithContent("Child One Success"))
                                .ThenNamed("ChildOneJoin");

                            childOneFork.When("ChildOneFailed")
                                .SignalReceived("ChildOneFailed")
                                .WriteHttpResponse(http => http
                                    .WithStatusCode(HttpStatusCode.OK)
                                    .WithContent("Child One Failed"))
                                .ThenNamed("ChildOneJoin");
                        }).Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("ChildOneJoin").ThenNamed("FinalJoin");

                    fork
                        .When("ChildTwo")
                        .Then<Fork>(activity => activity.WithBranches("ChildTwoSuccess", "ChildTwoFailed"), childTwoFork =>
                        {
                            childTwoFork
                                .When("ChildTwoSuccess")
                                .SignalReceived("ChildTwoSuccess")
                                .WriteHttpResponse(http => http
                                    .WithStatusCode(HttpStatusCode.OK)
                                    .WithContent("Child Two Success"))
                                .ThenNamed("ChildTwoJoin");

                            childTwoFork
                                .When("ChildTwoFailed")
                                .SignalReceived("ChildTwoFailed")
                                .WriteHttpResponse(http => http
                                    .WithStatusCode(HttpStatusCode.OK)
                                    .WithContent("Child Two Failed"))
                                .ThenNamed("ChildTwoJoin");
                        }).Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("ChildTwoJoin").ThenNamed("FinalJoin");
                })
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAll)).WithName("FinalJoin")
                .WriteLine("All set!");
        }
    }
}
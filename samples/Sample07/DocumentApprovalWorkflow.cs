using System;
using System.Net;
using System.Net.Http;
using Elsa;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Workflows;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample07
{
    public class DocumentApprovalWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<HttpRequestEvent>(
                    activity =>
                    {
                        activity.Method = HttpMethod.Post.Method;
                        activity.Path = new Uri("/documents", UriKind.RelativeOrAbsolute);
                        activity.ReadContent = true;
                    }
                )
                .Then<SetVariable>(
                    activity =>
                    {
                        activity.VariableName = "document";
                        activity.Expression = new JavaScriptExpression<object>("lastResult().ParsedContent");
                    }
                )
                .Then<SendEmail>(
                    activity =>
                    {
                        activity.From = new Literal("approval@acme.com");
                        activity.To = new JavaScriptExpression<string>("document.author.email");
                        activity.Subject = new JavaScriptExpression<string>("`Document received from ${document.author.name}`");
                        activity.Body = new JavaScriptExpression<string>(
                            "`Document from ${document.author.name} received for review. " +
                            "<a href=\"${signalUrl('approve')}\">Approve</a> or <a href=\"${signalUrl('reject')}\">Reject</a>`"
                        );
                    }
                )
                .Then<HttpResponseAction>(
                    activity =>
                    {
                        activity.Content = new Literal("<h1>Request for Approval Sent</h1><p>Your document has been received and will be reviewed shortly.</p>");
                        activity.ContentType = "text/html";
                        activity.StatusCode = HttpStatusCode.OK;
                        activity.ResponseHeaders = new Literal("X-Powered-By=Elsa Workflows");
                    }
                )
                .Then<Fork>(
                    activity => { activity.Branches = new[] { "Approve", "Reject" }; },
                    fork =>
                    {
                        fork
                            .When("Approve")
                            .Then<Signaled>(activity => activity.Signal = new Literal("approve"))
                            .Then("join-signals");

                        fork
                            .When("Reject")
                            .Then<Signaled>(activity => activity.Signal = new Literal("reject"))
                            .Then("join-signals");
                    }
                )
                .Add<Join>(activity => activity.Mode = Join.JoinMode.WaitAny, "join-signals")
                .Then<IfElse>(activity => activity.Expression = new JavaScriptExpression<bool>("input('signal') === 'approve'"),
                    ifElse =>
                    {
                        ifElse
                            .When(OutcomeNames.True)
                            .Then<SendEmail>(
                                activity =>
                                {
                                    activity.From = new Literal("approval@acme.com");
                                    activity.To = new JavaScriptExpression<string>("document.author.email");
                                    activity.Subject = new JavaScriptExpression<string>("`Document ${document.id} approved!`");
                                    activity.Body = new JavaScriptExpression<string>("`Great job ${document.author.name}, that document is perfect! Keep it up.`");
                                });
                        
                        ifElse
                            .When(OutcomeNames.False)
                            .Then<SendEmail>(
                                activity =>
                                {
                                    activity.From = new Literal("approval@acme.com");
                                    activity.To = new JavaScriptExpression<string>("document.author.email");
                                    activity.Subject = new JavaScriptExpression<string>("`Document ${document.id} rejected`");
                                    activity.Body = new JavaScriptExpression<string>("`Sorry ${document.author.name}, that document isn't good enough. Please try again.`");
                                });
                    });
        }
    }
}
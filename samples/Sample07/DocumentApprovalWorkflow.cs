using System;
using System.Net;
using System.Net.Http;
using Elsa;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
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
                        activity.From = new PlainTextExpression("approval@acme.com");
                        activity.To = new JavaScriptExpression<string>("document.author.email");
                        activity.Subject = new JavaScriptExpression<string>("`Document received from ${document.author.name}`");
                        activity.Body = new JavaScriptExpression<string>(
                            "`Document from ${document.author.name} received for review. " +
                            "<a href=\"${signalUrl('approve')}\">Approve</a> or <a href=\"${signalUrl('reject')}\">Reject</a>`"
                        );
                    }
                )
                .Then<HttpResponseTask>(
                    activity =>
                    {
                        activity.Content = new PlainTextExpression("<h1>Request for Approval Sent</h1><p>Your document has been received and will be reviewed shortly.</p>");
                        activity.ContentType = new PlainTextExpression("text/html");
                        activity.StatusCode = HttpStatusCode.OK;
                        activity.ResponseHeaders = new PlainTextExpression("X-Powered-By=Elsa Workflows");
                    }
                )
                .Then<Fork>(
                    activity => { activity.Branches = new[] { "Approve", "Reject" }; },
                    fork =>
                    {
                        fork
                            .When("Approve")
                            .Then<SignalEvent>(activity => activity.SignalName = "approve")
                            .Then("join-signals");

                        fork
                            .When("Reject")
                            .Then<SignalEvent>(activity => activity.SignalName = "reject")
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
                                    activity.From = new PlainTextExpression("approval@acme.com");
                                    activity.To = new JavaScriptExpression<string>("document.author.email");
                                    activity.Subject = new JavaScriptExpression<string>("`Document ${document.id} approved!`");
                                    activity.Body = new JavaScriptExpression<string>("`Great job ${document.author.name}, that document is perfect! Keep it up.`");
                                });
                        
                        ifElse
                            .When(OutcomeNames.False)
                            .Then<SendEmail>(
                                activity =>
                                {
                                    activity.From = new PlainTextExpression("approval@acme.com");
                                    activity.To = new JavaScriptExpression<string>("document.author.email");
                                    activity.Subject = new JavaScriptExpression<string>("`Document ${document.id} rejected`");
                                    activity.Body = new JavaScriptExpression<string>("`Sorry ${document.author.name}, that document isn't good enough. Please try again.`");
                                });
                    });
        }
    }
}
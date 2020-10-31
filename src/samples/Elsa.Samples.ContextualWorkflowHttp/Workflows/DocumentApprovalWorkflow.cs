using System;
using System.Net;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Samples.ContextualWorkflowHttp.Models;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.ContextualWorkflowHttp.Workflows
{
    /// <summary>
    /// Demonstrates loading & saving of the document-to-approve, which is the context (or subject) of the workflow.
    /// </summary>
    public class DocumentApprovalWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .StartWith<WriteLine>()
                    
                // The subject type of this workflow.
                .WithContextType<Document>()

                // Accept HTTP requests to submit new documents.
                .ReceiveHttpRequest(activity => activity.WithPath("/documents").WithMethod(HttpMethods.Post).WithTargetType<Document>())

                // Store the document as the workflow subject. It will be saved automatically when the workflow gets suspended.
                .Then(context => context.WorkflowExecutionContext.WorkflowContext = context.Input)

                // Correlate the workflow by document ID.
                .Correlate(context => ((Document)context.WorkflowExecutionContext.WorkflowContext)!.Id)

                // Write an HTTP response. 
                .WriteHttpResponse(
                    activity => activity
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithContentType("text/html")
                        .WithContent(
                            context =>
                            {
                                var document = (Document)context.WorkflowExecutionContext.WorkflowContext;
                                return $"Document received with ID {document!.Id}! Awaiting Approve or Reject response.";
                            }))

                // Fork execution into two branches: an Approve branch and a Reject branch.
                .Then<Fork>(
                    fork => fork.WithBranches("Approve", "Reject"),
                    fork =>
                    {
                        var approveBranch = fork
                            .When("Approve")
                            .ReceiveHttpRequest(activity => activity.WithPath("/approve").WithMethod(HttpMethods.Post).WithTargetType<Comment>())
                            .Then(StoreComment);

                        var rejectBranch = fork
                            .When("Reject")
                            .ReceiveHttpRequest(activity => activity.WithPath("/reject").WithMethod(HttpMethods.Post).WithTargetType<Comment>())
                            .Then(StoreComment);

                        WriteResponse(approveBranch, document => $"Thanks for approving document {document!.Id}!").Then("Join");
                        WriteResponse(rejectBranch, document => $"Thanks for rejecting document {document!.Id}!");
                    });
        }

        private void StoreComment(ActivityExecutionContext context)
        {
            var document = (Document)context.WorkflowExecutionContext.WorkflowContext;
            var comment = (Comment)((HttpRequestModel)context.Input)!.Body;
            document!.Comments.Add(comment);
        }

        private IActivityBuilder WriteResponse(IActivityBuilder builder, Func<Document, string> html) =>
            builder.WriteHttpResponse(
                activity => activity
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContentType("text/html")
                    .WithContent(
                        context =>
                        {
                            var document = (Document)context.WorkflowExecutionContext.WorkflowContext;
                            return html(document);
                        }));
    }
}
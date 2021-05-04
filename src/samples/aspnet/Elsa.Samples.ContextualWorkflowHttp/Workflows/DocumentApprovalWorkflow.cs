using System;
using System.Net;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Samples.ContextualWorkflowHttp.Models;
using Elsa.Services.Models;

namespace Elsa.Samples.ContextualWorkflowHttp.Workflows
{
    /// <summary>
    /// Demonstrates saving & loading of the document-to-approve, which is the context (or subject) of the workflow.
    /// </summary>
    public class DocumentApprovalWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            // Demonstrating that we can create activities and connect to them later on by using the activity builder reference.
            var join = builder.Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny));
            join.Finish();

            builder
                // The workflow context type of this workflow.
                .WithContextType<Document>()

                // Accept HTTP requests to submit new documents.
                .HttpEndpoint<Document>("/documents")

                // Store the document as the workflow context. It will be saved automatically when the workflow gets suspended.
                .Then(context => context.SetWorkflowContext(context.GetInput<HttpRequestModel>()!.GetBody<Document>())).LoadWorkflowContext()

                // Write an HTTP response. 
                .WriteHttpResponse(
                    activity => activity
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithContentType("text/html")
                        .WithContent(context => $"Document received with ID {GetDocumentId(context)}! Awaiting Approve or Reject response."))

                // Fork execution into two branches: an Approve branch and a Reject branch.
                .Then<Fork>(
                    fork => fork.WithBranches("Approve", "Reject"),
                    fork =>
                    {
                        var approveBranch = fork
                            .When("Approve")
                            .HttpEndpoint<Comment>(context => $"/documents/{GetDocumentId(context)}/approve")
                            .Then(StoreComment);

                        var rejectBranch = fork
                            .When("Reject")
                            .HttpEndpoint<Comment>(context => $"/documents/{GetDocumentId(context)}/reject")
                            .Then(StoreComment);

                        WriteResponse(approveBranch, document => $"Thanks for approving document {document!.DocumentId}!").Then(join);
                        WriteResponse(rejectBranch, document => $"Thanks for rejecting document {document!.DocumentId}!").Then(join);
                    });
        }

        private static Document GetDocument(ActivityExecutionContext context) => context.GetWorkflowContext<Document>();
        private static string GetDocumentId(ActivityExecutionContext context) => GetDocument(context).DocumentId;

        private static void StoreComment(ActivityExecutionContext context)
        {
            var document = (Document) context.WorkflowExecutionContext.WorkflowContext!;
            var comment = (Comment) ((HttpRequestModel) context.Input!).Body!;
            document.Comments.Add(comment);
        }

        private static IActivityBuilder WriteResponse(IBuilder builder, Func<Document, string> html) =>
            builder.WriteHttpResponse(
                activity => activity
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContentType("text/html")
                    .WithContent(
                        context =>
                        {
                            var document = GetDocument(context);
                            return html(document);
                        }));
    }
}
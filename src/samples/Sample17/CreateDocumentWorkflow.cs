using Elsa.Activities.Console.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Sample17.Activities;

namespace Sample17
{
    public class CreateDocumentWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter a title"))
                .Then<ReadLine>(id: "DocumentTitleInput")
                .Then<CreateDocument>(x => x.TitleExpression = new JavaScriptExpression<string>("DocumentTitleInput.Input"), id: "CreateDocument")
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`A new document was created with title \"${CreateDocument.Document.Title}\"`"));
        }
    }
}
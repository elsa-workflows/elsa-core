using Elsa.Activities.Console.Activities;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Sample17.Activities;
using Sample17.Models;

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
                .Then<SetVariable>(x =>
                {
                    x.VariableName = "Document";
                    x.ValueExpression = new JavaScriptExpression<Document>("CreateDocument.Document");
                })
                .Then<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`A new document was created with title \"${Document.Title}\"`"));
        }
    }
}
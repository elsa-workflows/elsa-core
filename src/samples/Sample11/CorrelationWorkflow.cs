using Elsa.Activities.Console.Activities;
using Elsa.Activities.Workflows.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample11
{
    public class CorrelationWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(activity => activity.TextExpression = new JavaScriptExpression<string>("`Workflow started with correlation ID \"${correlationId()}\".`"))
                .Then<Signaled>(activity => activity.Signal = new LiteralExpression("Proceed"))
                .Then<WriteLine>(activity => activity.TextExpression = new JavaScriptExpression<string>("`Signal received for workflow with correlation ID: \"${correlationId()}\"`"))
                .Then<WriteLine>(activity => activity.TextExpression = new LiteralExpression("Workflow finished."));
        }
    }
}
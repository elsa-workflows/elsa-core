using Elsa.Services.Models;
using Fluid;
using MediatR;

namespace Elsa.Scripting.Liquid.Messages
{
    public class EvaluatingLiquidExpression : INotification
    {
        public EvaluatingLiquidExpression(TemplateContext templateContext, WorkflowExecutionContext workflowExecutionContext)
        {
            TemplateContext = templateContext;
            WorkflowExecutionContext = workflowExecutionContext;
        }

        public TemplateContext TemplateContext { get; }
        public WorkflowExecutionContext WorkflowExecutionContext { get; }
    }
}
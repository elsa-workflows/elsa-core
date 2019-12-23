using Elsa.Services.Models;
using Fluid;
using MediatR;

namespace Elsa.Scripting.Liquid.Messages
{
    public class EvaluatingLiquidExpression : INotification
    {
        public EvaluatingLiquidExpression(TemplateContext templateContext, ActivityExecutionContext context)
        {
            TemplateContext = templateContext;
            Context = context;
        }

        public TemplateContext TemplateContext { get; }
        public ActivityExecutionContext Context { get; }
    }
}
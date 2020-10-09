using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services.Models;
using Fluid;
using MediatR;

namespace Elsa.Scripting.Liquid.Services
{
    public class LiquidExpressionHandler : IExpressionHandler
    {
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly IMediator _mediator;

        public LiquidExpressionHandler(ILiquidTemplateManager liquidTemplateManager, IMediator mediator)
        {
            this._liquidTemplateManager = liquidTemplateManager;
            this._mediator = mediator;
        }

        public string Type => LiquidExpression.ExpressionType;

        public async Task<object> EvaluateAsync(IWorkflowExpression expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var liquidExpression = (LiquidExpression)expression;
            var templateContext = await CreateTemplateContextAsync(context);
            var result = await _liquidTemplateManager.RenderAsync(liquidExpression.Expression, templateContext);
            return string.IsNullOrWhiteSpace(result) ? default : Convert.ChangeType(result, returnType);
        }

        private async Task<TemplateContext> CreateTemplateContextAsync(ActivityExecutionContext workflowContext)
        {
            var context = new TemplateContext();
            context.SetValue("WorkflowExecutionContext", workflowContext);
            await _mediator.Publish(new EvaluatingLiquidExpression(context, workflowContext));
            context.Model = workflowContext;
            return context;
        }
    }
}
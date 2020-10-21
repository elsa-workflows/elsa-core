using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services;
using Elsa.Services.Models;
using Fluid;
using MediatR;

namespace Elsa.Scripting.Liquid.Services
{
    public class LiquidExpressionEvaluator : IExpressionEvaluator
    {
        private readonly ILiquidTemplateManager liquidTemplateManager;
        private readonly IMediator mediator;
        public const string SyntaxName = "Liquid";

        public LiquidExpressionEvaluator(ILiquidTemplateManager liquidTemplateManager, IMediator mediator)
        {
            this.liquidTemplateManager = liquidTemplateManager;
            this.mediator = mediator;
        }

        public string Syntax => SyntaxName;

        public async Task<object> EvaluateAsync(string expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var templateContext = await CreateTemplateContextAsync(workflowExecutionContext, cancellationToken);
            var result = await liquidTemplateManager.RenderAsync(expression, templateContext);
            return string.IsNullOrWhiteSpace(result) ? default : type != null ? Convert.ChangeType(result, type) : result;
        }

        private async Task<TemplateContext> CreateTemplateContextAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var context = new TemplateContext();
            context.SetValue("WorkflowExecutionContext", workflowContext);
            await mediator.Publish(new EvaluatingLiquidExpression(context, workflowContext), cancellationToken);
            context.Model = workflowContext;
            return context;
        }
    }
}
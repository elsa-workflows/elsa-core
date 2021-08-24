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
    public class LiquidHandler : IExpressionHandler
    {
        public const string SyntaxName = "Liquid";
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly IMediator _mediator;

        public LiquidHandler(ILiquidTemplateManager liquidTemplateManager, IMediator mediator)
        {
            _liquidTemplateManager = liquidTemplateManager;
            _mediator = mediator;
        }

        public string Syntax => SyntaxName;

        public async Task<object?> EvaluateAsync(string expression, Type returnType, ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var templateContext = await CreateTemplateContextAsync(context, cancellationToken);
            var result = await _liquidTemplateManager.RenderAsync(expression, templateContext);
            return string.IsNullOrWhiteSpace(result) ? default : result.Parse(returnType);
        }

        private async Task<TemplateContext> CreateTemplateContextAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var context = new TemplateContext(new TemplateOptions());
            context.SetValue("ActivityExecutionContext", activityExecutionContext);
            context.Model = activityExecutionContext;
            await _mediator.Publish(new EvaluatingLiquidExpression(context, activityExecutionContext), cancellationToken);
            return context;
        }
    }
}
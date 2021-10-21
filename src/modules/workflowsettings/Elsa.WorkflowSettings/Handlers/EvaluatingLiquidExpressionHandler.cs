using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services.Models;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Services;
using Fluid;
using Fluid.Values;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.WorkflowSettings.Handlers
{
    public class EvaluatingLiquidExpressionHandler : INotificationHandler<EvaluatingLiquidExpression>
    {
        private readonly IWorkflowSettingsManager _workflowSettingsManager;
        public EvaluatingLiquidExpressionHandler(IWorkflowSettingsManager workflowSettingsManager)
        {
            _workflowSettingsManager = workflowSettingsManager;
        }

        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;
            var options = context.Options;
            var memberAccessStrategy = options.MemberAccessStrategy;

            memberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Settings", x => new LiquidPropertyAccessor(async name => await ToFluidValue(await GetWorkflowSettings(x.WorkflowExecutionContext, cancellationToken), name, options)));

            return Task.CompletedTask;
        }

        private Task<FluidValue> ToFluidValue(IEnumerable<WorkflowSetting> settings, string key, TemplateOptions options) => Task.FromResult(!settings.Any(x => x.Key == key) ? NilValue.Instance : FluidValue.Create(settings.First(x => x.Key == key).GetValue(), options));

        private async Task<IEnumerable<WorkflowSetting>> GetWorkflowSettings(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var workflowBlueprintId = context.WorkflowBlueprint.Id;

            return await _workflowSettingsManager.LoadSettingsAsync(workflowBlueprintId, cancellationToken);
        }
    }
}

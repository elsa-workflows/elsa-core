using Elsa.Scripting.JavaScript.Messages;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Services;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.WorkflowSettings.Handlers
{
    public class EvaluatingJavaScriptExpressionHandler : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly IWorkflowSettingsManager _workflowSettingsManager;

        public EvaluatingJavaScriptExpressionHandler(IWorkflowSettingsManager workflowSettingsManager)
        {
            _workflowSettingsManager = workflowSettingsManager;
        }

        public async Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;

            var workflowBlueprintId = notification.ActivityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint.Id;

            var settings = await _workflowSettingsManager.LoadSettingsAsync(workflowBlueprintId, cancellationToken);

            engine.SetValue("settings", WorkFlowSettingsAsDictionary(settings));
        }

        Dictionary<string, string?> WorkFlowSettingsAsDictionary(IEnumerable<WorkflowSetting> settings)
        {
            var result = new Dictionary<string, string?>();

            foreach (var setting in settings)
            {
                result.Add(setting.Key, setting.Value ?? setting.DefaultValue);
            }

            return result;
        }
    }
}
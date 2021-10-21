using Elsa.Scripting.JavaScript.Events;
using Elsa.WorkflowSettings.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.WorkflowSettings.Handlers
{
    public class RenderingTypeScriptDefinitionsHandler : INotificationHandler<RenderingTypeScriptDefinitions>
    {
        private readonly IWorkflowSettingsManager _workflowSettingsManager;

        public RenderingTypeScriptDefinitionsHandler(IWorkflowSettingsManager workflowSettingsManager)
        {
            _workflowSettingsManager = workflowSettingsManager;
        }

        public async Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            var output = notification.Output;

            var workflowDefinition = notification.WorkflowDefinition;

            if (workflowDefinition == null) return;

            var settings = await _workflowSettingsManager.LoadSettingsAsync(workflowDefinition.DefinitionId, cancellationToken);

            if (settings == null) return;

            output.AppendLine("declare interface Settings {");

            foreach (var setting in settings)
            {
                var value = setting.Value ?? setting.DefaultValue;

                var typeScriptType = notification.GetTypeScriptType(value?.GetType() ?? typeof(string));

                output.AppendLine($"{setting.Key}: {typeScriptType};");
            }

            output.AppendLine("}");
            output.AppendLine("declare const settings: Settings");
        }
    }
}

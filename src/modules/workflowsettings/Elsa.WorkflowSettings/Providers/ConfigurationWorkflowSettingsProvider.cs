using System.Threading;
using System.Threading.Tasks;
using Elsa.WorkflowSettings.Models;
using Microsoft.Extensions.Configuration;

namespace Elsa.WorkflowSettings.Providers
{
    public class ConfigurationWorkflowSettingsProvider : WorkflowSettingsProvider
    {
        private readonly IConfiguration _configuration;

        public ConfigurationWorkflowSettingsProvider(IConfiguration configuarion)
        {
            _configuration = configuarion;
        }

        public override ValueTask<WorkflowSetting> GetWorkflowSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken)
        {
            var value = _configuration[$"{workflowBlueprintId}:{key}"];
            return new ValueTask<WorkflowSetting>(new WorkflowSetting { Value = value });
        }
    }
}
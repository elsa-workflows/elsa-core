using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.WorkflowSettings.Providers
{
    public class ConfigurationWorkflowSettingsProvider : WorkflowSettingsProvider
    {
        public override int Priority => 2;

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

        public override ValueTask<IEnumerable<WorkflowSetting>> GetWorkflowSettingsAsync(string workflowBlueprintId, CancellationToken cancellationToken = default, IOrderBy<WorkflowSetting>? orderBy = default, IPaging? paging = default)
        {
            var values = _configuration.GetSection($"{workflowBlueprintId}").Get<Dictionary<string, string>>();

            var settings = values?.Select(x => new WorkflowSetting() { Key = x.Key, Value = x.Value });

            return new ValueTask<IEnumerable<WorkflowSetting>>(settings ?? new List<WorkflowSetting>());
        }
    }
}
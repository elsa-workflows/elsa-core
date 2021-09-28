using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Models;

namespace Elsa.WorkflowSettings.Services
{
    public interface IWorkflowSettingsManager
    {
        Task<WorkflowSetting> LoadSettingAsync(string workflowBlueprintId, string key, CancellationToken cancellationToken = default);
        Task<IEnumerable<WorkflowSetting>> LoadSettingsAsync(string workflowBlueprintId, CancellationToken cancellationToken = default, IOrderBy<WorkflowSetting>? orderBy = default, IPaging? paging = default);
    }
}
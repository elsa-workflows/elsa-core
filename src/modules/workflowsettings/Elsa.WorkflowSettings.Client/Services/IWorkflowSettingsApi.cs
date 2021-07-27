using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Elsa.WorkflowSettings.Client.Models;
using Refit;

namespace Elsa.WorkflowSettings.Client.Services
{
    public interface IWorkflowSettingsApi
    {
        [Get("/v1/workflow-settings")]
        Task<IEnumerable<WorkflowSetting>> ListAsync(CancellationToken cancellationToken = default);

        [Get("/v1/workflow-settings")]
        Task<PagedList<WorkflowSetting>> ListAsync(int? page = default, int? pageSize = default, CancellationToken cancellationToken = default);

        [Post("/v1/workflow-settings")]
        Task<WorkflowSetting> SaveAsync([Body] SaveWorkflowSettingsRequest request, CancellationToken cancellationToken = default);

        [Delete("/v1/workflow-settings/{workflowSettingsId}")]
        Task DeleteAsync(string workflowSettingsId, CancellationToken cancellationToken = default);
    }
}

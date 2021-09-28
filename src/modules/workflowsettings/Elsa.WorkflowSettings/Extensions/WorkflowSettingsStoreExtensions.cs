using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence;
using Elsa.WorkflowSettings.Persistence.Specification.WorkflowSettingsDefinitions;

namespace Elsa.WorkflowSettings.Extensions
{
    public static class WorkflowSettingsStoreExtensions
    {
        public static Task<WorkflowSetting?> FindByIdAsync(
           this IWorkflowSettingsStore store,
           string id,
           CancellationToken cancellationToken = default) =>
           store.FindAsync(new WorkflowSettingsIdSpecification(id), cancellationToken);

        public static Task<WorkflowSetting?> FindByWorkflowBlueprintIdAndKeyAsync(
            this IWorkflowSettingsStore store,
            string workflowBlueprintId,
            string key,
            CancellationToken cancellationToken = default) =>
            store.FindAsync(new WorkflowSettingsBlueprintIdAndKeySpecification(workflowBlueprintId, key), cancellationToken);

        public static Task<IEnumerable<WorkflowSetting>> FindByWorkflowBlueprintIdAsync(
            this IWorkflowSettingsStore store,
            string workflowBlueprintId,
            CancellationToken cancellationToken = default,
            IOrderBy<WorkflowSetting>? orderBy = default,
            IPaging? paging = default) =>
            store.FindManyAsync(new WorkflowSettingsBlueprintIdSpecification(workflowBlueprintId), orderBy, paging, cancellationToken);
    }
}
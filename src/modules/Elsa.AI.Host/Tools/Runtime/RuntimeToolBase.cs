using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.AI.Host.Tools;
using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.AI.Host.Tools.Runtime;

public abstract class RuntimeToolBase(IServiceProvider serviceProvider, AIGroundingResultFormatter formatter) : GroundingToolBase
{
    protected AIGroundingResultFormatter Formatter { get; } = formatter;
    protected IWorkflowInstanceStore? WorkflowInstanceStore => serviceProvider.GetService(typeof(IWorkflowInstanceStore)) as IWorkflowInstanceStore;

    protected AIToolResult InstanceStoreUnavailable() =>
        Formatter.Unavailable("Workflow instance store");

    protected static bool IsTenantAllowed(WorkflowInstance instance, string? tenantId) =>
        string.Equals(NormalizeTenant(instance.TenantId), NormalizeTenant(tenantId), StringComparison.Ordinal);

    protected static WorkflowInstanceFilter CreateInstanceFilter(JsonObject arguments)
    {
        var filter = new WorkflowInstanceFilter
        {
            Id = GetString(arguments, "instanceId") ?? GetString(arguments, "id"),
            DefinitionId = GetString(arguments, "definitionId"),
            DefinitionVersionId = GetString(arguments, "definitionVersionId"),
            CorrelationId = GetString(arguments, "correlationId"),
            SearchTerm = GetString(arguments, "query") ?? GetString(arguments, "searchTerm"),
            HasIncidents = GetBool(arguments, "hasIncidents")
        };

        if (Enum.TryParse<WorkflowStatus>(GetString(arguments, "status"), true, out var status))
            filter.WorkflowStatus = status;

        if (Enum.TryParse<WorkflowSubStatus>(GetString(arguments, "subStatus"), true, out var subStatus))
            filter.WorkflowSubStatus = subStatus;

        var from = GetDateTimeOffset(arguments, "from");
        var to = GetDateTimeOffset(arguments, "to");
        var timestampFilters = new List<TimestampFilter>();
        if (from != null)
            timestampFilters.Add(new TimestampFilter
            {
                Column = nameof(WorkflowInstance.UpdatedAt),
                Operator = TimestampFilterOperator.GreaterThanOrEqual,
                Timestamp = from.Value
            });
        if (to != null)
            timestampFilters.Add(new TimestampFilter
            {
                Column = nameof(WorkflowInstance.UpdatedAt),
                Operator = TimestampFilterOperator.LessThanOrEqual,
                Timestamp = to.Value
            });
        if (timestampFilters.Count > 0)
            filter.TimestampFilters = timestampFilters;

        return filter;
    }

    protected async ValueTask<WorkflowInstance?> FindAuthorizedInstanceAsync(JsonObject arguments, string? tenantId, CancellationToken cancellationToken)
    {
        var store = WorkflowInstanceStore;
        if (store == null)
            return null;

        var instance = await store.FindAsync(CreateInstanceFilter(arguments), cancellationToken);
        return instance != null && IsTenantAllowed(instance, tenantId) ? instance : null;
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonObject arguments, string name) =>
        arguments.TryGetPropertyValue(name, out var node) && node is JsonValue value && value.TryGetValue<DateTimeOffset>(out var result)
            ? result
            : null;

    private static string NormalizeTenant(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? "" : tenantId;
}

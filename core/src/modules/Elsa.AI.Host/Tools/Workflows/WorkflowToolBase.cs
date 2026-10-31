using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.AI.Host.Tools;
using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;

namespace Elsa.AI.Host.Tools.Workflows;

public abstract class WorkflowToolBase(IServiceProvider serviceProvider, AIGroundingResultFormatter formatter) : GroundingToolBase
{
    protected IServiceProvider ServiceProvider { get; } = serviceProvider;
    protected AIGroundingResultFormatter Formatter { get; } = formatter;
    protected IWorkflowDefinitionStore? WorkflowDefinitionStore => ServiceProvider.GetService(typeof(IWorkflowDefinitionStore)) as IWorkflowDefinitionStore;

    protected AIToolResult WorkflowStoreUnavailable() =>
        Formatter.Unavailable("Workflow definition store");

    protected static bool IsTenantAllowed(WorkflowDefinition definition, string? tenantId) =>
        string.Equals(NormalizeTenant(definition.TenantId), NormalizeTenant(tenantId), StringComparison.Ordinal);

    protected static WorkflowDefinitionFilter CreateDefinitionFilter(JsonObject arguments)
    {
        var filter = new WorkflowDefinitionFilter
        {
            Id = GetString(arguments, "versionId") ?? GetString(arguments, "id"),
            DefinitionId = GetString(arguments, "definitionId"),
            SearchTerm = GetString(arguments, "query") ?? GetString(arguments, "searchTerm"),
            Name = GetString(arguments, "name")
        };

        var version = GetInt(arguments, "version");
        if (version != null)
            filter.VersionOptions = VersionOptions.SpecificVersion(version.Value);
        else if (GetBool(arguments, "published") == true)
            filter.VersionOptions = VersionOptions.Published;
        else
            filter.VersionOptions = VersionOptions.Latest;

        return filter;
    }

    protected static string NormalizeTenant(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? "" : tenantId;
}

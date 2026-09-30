using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

[PublicAPI]
internal class List(IActivityRegistry registry, IActivityRegistryPopulator registryPopulator) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/descriptors/activities");

        // The plain read lists the activities the designer needs to open any definition, so every signed-in user may read it.
        // That includes activities built from stored workflow definitions that are marked as usable as an activity.
        RequireAuthenticatedOnly();
    }

    public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var forceRefresh = Query<bool>("refresh", false);

        if (forceRefresh && CanRefresh())
            await registryPopulator.PopulateRegistryAsync(cancellationToken);

        var descriptors = registry.ListAll().ToList();
        var response = new Response(descriptors);

        return response;
    }

    // Studio always sends refresh=true. Rebuilding the registry is expensive, so the flag is honoured only for callers
    // holding the activities permission and silently ignored for everyone else, who get the current registry.
    private bool CanRefresh()
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            return true;

        var evaluator = HttpContext.GetPermissionEvaluator();
        return evaluator.HasPermission(User, Elsa.Workflows.Api.Permissions.WorkflowPermissions.DescriptorsActivities, CoreVerbs.View);
    }
}

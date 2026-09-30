using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.List;

[PublicAPI]
internal class List(IActivityRegistry registry, IActivityRegistryPopulator registryPopulator) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/descriptors/activities");

        // The plain read lists the activities the designer needs to open any definition, so every signed-in user may read it.
        // That includes activities built from stored workflow definitions that are marked as usable as an activity.
        // The refresh flag rebuilds the registry, so it is checked against the permission in HandleAsync.
        RequireAuthenticatedOnly();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var forceRefresh = Query<bool>("refresh", false);

        if (forceRefresh)
        {
            if (!CanRefresh())
            {
                await Send.ForbiddenAsync(cancellationToken);
                return;
            }

            await registryPopulator.PopulateRegistryAsync(cancellationToken);
        }

        var descriptors = registry.ListAll().ToList();

        await Send.OkAsync(new Response(descriptors), cancellationToken);
    }

    private bool CanRefresh()
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            return true;

        var evaluator = HttpContext.RequestServices.GetService<IPermissionEvaluator>() ?? PermissionEvaluator.Shared;
        return evaluator.HasPermission(User, Elsa.Workflows.Api.Permissions.WorkflowPermissions.DescriptorsActivities, CoreVerbs.View);
    }
}

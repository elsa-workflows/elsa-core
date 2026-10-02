using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.UserTasks.Client;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Elsa.Studio.UserTasks.Menu;

public sealed class UserTasksMenu(
    ILocalizer localizer,
    IRemoteFeatureProvider remoteFeatureProvider,
    IBackendApiClientProvider backendApiClientProvider,
    ILogger<UserTasksMenu> logger) : IWorkflowMenuContributor
{
    public async ValueTask<IEnumerable<MenuItem>> GetWorkflowMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await remoteFeatureProvider.IsEnabledOrDefaultAsync(Feature.RemoteFeatureName, cancellationToken))
            return [];

        try
        {
            var api = await backendApiClientProvider.GetApiAsync<IUserTasksApi>(cancellationToken);
            var capabilities = await api.GetCapabilitiesAsync(cancellationToken);
            if (!capabilities.Enabled || !capabilities.CanList || !capabilities.CanRead)
                return [];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return [];
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "User Tasks capabilities could not be loaded; hiding the menu.");
            return [];
        }

        return
        [
            new MenuItem
            {
                Text = localizer["User Tasks"],
                Href = "workflows/user-tasks",
                Icon = Icons.Material.Outlined.TaskAlt,
                GroupName = MenuItemGroups.General.Name
            }
        ];
    }
}

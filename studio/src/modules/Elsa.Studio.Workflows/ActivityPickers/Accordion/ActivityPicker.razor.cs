using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.ActivityPickers.Accordion;

/// <summary>
/// A component that allows the user to pick an activity.
/// </summary>
public partial class ActivityPicker
{
    [Parameter]
    public Func<string, string> CategoryDisplayResolver { get; set; } = null!;

    private string _searchText = "";

    private IEnumerable<IGrouping<string, ActivityDescriptor>> GroupedActivityDescriptors
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return ActivityDescriptors.GroupBy(x => x.Category); // Return all items grouped by category

            var items = ActivityDescriptors.Where(item =>
                item.Name.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase) ||
                item.TypeName.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase) ||
                (item.DisplayName != null && item.DisplayName.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase)) ||
                item.Category.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase) ||
                (item.Description != null && item.Description.Contains(_searchText, StringComparison.InvariantCultureIgnoreCase))
            );
            return items
                .GroupBy(x => x.Category);
        }
    }

    /// <summary>
    /// The drag and drop manager.
    /// </summary>
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRemoteFeatureProvider RemoteFeatureProvider { get; set; } = default!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = default!;

    private IEnumerable<ActivityDescriptor> ActivityDescriptors { get; set; } = new List<ActivityDescriptor>();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await Refresh();
    }

    private async Task Refresh()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        var descriptors = ActivityRegistry.ListBrowsable();
        var backendContextKey = BackendApiClientProvider.ContextKey;
        var supportsCompensationDrafts = await NativeCompensationDraftCapability.IsEnabledForContextAsync(
            RemoteFeatureProvider, BackendApiClientProvider, backendContextKey);
        ActivityDescriptors = supportsCompensationDrafts
            ? descriptors
            : descriptors.Where(x => !NativeCompensationDraftCapability.IsCompensationScopeActivity(x.TypeName));
        StateHasChanged();
    }

    private Task OnDragStart(ActivityDescriptor activityDescriptor)
    {
        // Native descriptors can only be dragged if they survived the current filtered picker view.
        // Creation boundaries repeat the fresh check, so dragstart stays synchronous and cannot lose a drop.
        var nativeDescriptorIsOffered =
            !NativeCompensationDraftCapability.IsCompensationScopeActivity(activityDescriptor.TypeName) ||
            ActivityDescriptors.Any(x => ReferenceEquals(x, activityDescriptor));
        DragDropManager.Payload = nativeDescriptorIsOffered ? activityDescriptor : null;
        return Task.CompletedTask;
    }
}

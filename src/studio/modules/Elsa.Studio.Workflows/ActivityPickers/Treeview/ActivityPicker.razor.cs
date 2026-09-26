using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.ActivityPickers.Treeview;

/// <summary>
/// A component that allows the user to pick an activity.
/// </summary>
public partial class ActivityPicker
{
    private bool _expanded;
    private string _searchText = "";
    private MudTreeView<string> _treeView = null!;
    private readonly List<ITreeItemData<string>> _treeItemData = [];

    private IEnumerable<ActivityDescriptor> ActivityDescriptors { get; set; } = new List<ActivityDescriptor>();

    /// <summary>
    /// The drag and drop manager.
    /// </summary>
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;

    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await Refresh();
    }

    private async Task Refresh()
    {
        await ActivityRegistry.EnsureLoadedAsync();
        ActivityDescriptors = ActivityRegistry.ListBrowsable();
        BuildTree();
        StateHasChanged();
    }

    private void BuildTree()
    {
        _treeItemData.Clear();

        foreach (var activity in ActivityDescriptors)
        {
            var categories = (activity.Category?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                .Select(c => c.Trim())
                .ToArray();

            var currentLevel = _treeItemData;

            // Traverse or create category nodes
            foreach (var category in categories)
            {
                var node = FindOrCreateCategoryNode(currentLevel, category, activity.Category);
                currentLevel = node.ChildrenList;
            }

            // Add the activity node
            var displaySettings = ActivityDisplaySettingsRegistry.GetSettings(activity.TypeName);
            var activityNode = new ActivityTreeItem(activity.DisplayName ?? activity.Name)
            {
                ActivityDescriptor = activity,
                CategoryPath = activity.Category ?? string.Empty,
                Icon = displaySettings.Icon,
                IconColor = displaySettings.Color
            };

            // Insert node in an alphabetically sorted order
            InsertSorted(currentLevel, activityNode);
        }
    }

    private ActivityTreeItem FindOrCreateCategoryNode(List<ITreeItemData<string>> level, string category, string? path)
    {
        var node = level.OfType<ActivityTreeItem>().FirstOrDefault(x => string.Equals(x.Text, category, StringComparison.OrdinalIgnoreCase));
        if (node == null)
        {
            node = new(category) { CategoryPath = path ?? string.Empty };
            InsertSorted(level, node);
        }
        return node;
    }

    private void InsertSorted(List<ITreeItemData<string>> list, ActivityTreeItem node)
    {
        var index = list.OfType<ActivityTreeItem>()
            .ToList()
            .FindIndex(x => string.Compare(node.Text ?? string.Empty, x.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase) < 0);

        if (index == -1)
            list.Add(node);
        else
            list.Insert(index, node);
    }

    private async void OnTextChanged(string searchPhrase)
    {
        _searchText = searchPhrase;
        await _treeView.FilterAsync();
        _expanded = true;
    }

    private async void OnExpandClicked()
    {
        if (_expanded)
            await _treeView.CollapseAllAsync();
        else
            await _treeView.ExpandAllAsync();
        _expanded = !_expanded;
    }

    private Task<bool> MatchesName(ActivityTreeItem item)
    {
        if (string.IsNullOrEmpty(item.CategoryPath) && string.IsNullOrEmpty(item.Text))
            return Task.FromResult(false);

        return Task.FromResult(item.CategoryPath.Contains(_searchText, StringComparison.OrdinalIgnoreCase) || (item.Text?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    private void OnDragStart(ActivityDescriptor activityDescriptor)
    {
        DragDropManager.Payload = activityDescriptor;
    }
}

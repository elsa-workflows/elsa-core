using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

/// <summary>
/// Contextual activity command palette for StateMachine lifecycle and transition slots.
/// </summary>
public partial class StateMachineActivityPickerDialog
{
    private const string SuggestedSection = "suggested";
    private const string RecentSection = "recent";
    private const string AllSection = "all";
    private readonly string _id = $"state-machine-activity-picker-{Guid.NewGuid():N}";
    private IReadOnlyList<ActivityDescriptor> _descriptors = [];
    private IReadOnlySet<string> _rootActivityTypeNames = new HashSet<string>(StringComparer.Ordinal);
    private string _searchText = string.Empty;
    private string _selectedSection = SuggestedSection;
    private ActivityDescriptor? _selectedDescriptor;
    private Exception? _loadError;
    private bool _isLoading = true;
    private bool _focusSearch;
    private ElementReference _searchInput;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IWorkflowRootActivityTemplateProvider RootActivityTemplateProvider { get; set; } = null!;
    [Inject] private ILocalizer Localizer { get; set; } = null!;

    [Parameter] public string SlotName { get; set; } = "action";
    [Parameter] public bool IsReplacing { get; set; }
    [Parameter] public IReadOnlyCollection<string> RecentActivityTypes { get; set; } = [];

    /// <summary>
    /// Restricts the offered activities to the descriptors this predicate accepts, in place of the default set (every
    /// browsable activity plus the designer-backed root activities). Lets a host other than the StateMachine designer
    /// reuse this picker for a narrower purpose — the BPMN "Performed by" section offers only leaf work.
    /// </summary>
    [Parameter] public Func<ActivityDescriptor, bool>? DescriptorFilter { get; set; }

    /// <summary>Replaces the short context label derived from <see cref="SlotName"/> (e.g. <c>THEN</c>).</summary>
    [Parameter] public string? ContextLabel { get; set; }

    /// <summary>Replaces the context sentence derived from <see cref="SlotName"/> (e.g. <c>Runs after source exit</c>).</summary>
    [Parameter] public string? ContextHint { get; set; }

    private string SearchInputId => $"{_id}-search";
    private string ResultsId => $"{_id}-results";
    private string DetailsId => $"{_id}-details";
    private string PickerHeading => IsReplacing
        ? Localizer[$"Choose a replacement {SlotDisplayName} activity"]
        : Localizer[$"Choose {SlotArticle} {SlotDisplayName} activity"];
    private string SlotArticle => SlotName.Equals("trigger", StringComparison.OrdinalIgnoreCase) ? Localizer["a"] : Localizer["an"];
    private string SlotDisplayName => SlotName.ToLowerInvariant() switch
    {
        "entry" => Localizer["entry"],
        "exit" => Localizer["exit"],
        "trigger" => Localizer["trigger"],
        _ => Localizer["action"]
    };
    private string ContextKicker => ContextLabel ?? SlotName.ToLowerInvariant() switch
    {
        "entry" => Localizer["ON ENTRY"],
        "exit" => Localizer["ON EXIT"],
        "trigger" => Localizer["WHEN"],
        _ => Localizer["THEN"]
    };
    private string ContextDescription => ContextHint ?? SlotName.ToLowerInvariant() switch
    {
        "entry" => Localizer["Runs when this state becomes active"],
        "exit" => Localizer["Runs before an accepted transition leaves this state"],
        "trigger" => Localizer["Starts the transition"],
        _ => Localizer["Runs after source exit"]
    };

    private IReadOnlyList<ActivityDescriptor> SuggestedDescriptors =>
        string.Equals(SlotName, "trigger", StringComparison.OrdinalIgnoreCase)
            ? []
            : _descriptors
                .Where(IsDesignerBackedRoot)
                .OrderBy(x => ReferenceEquals(x, SequenceDescriptor) ? 0 : 1)
                .ThenBy(GetDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

    private IReadOnlyList<ActivityDescriptor> RecentDescriptors => RecentActivityTypes
        .Select(type => _descriptors.FirstOrDefault(x => string.Equals(x.TypeName, type, StringComparison.Ordinal)))
        .Where(x => x != null)
        .Cast<ActivityDescriptor>()
        .DistinctBy(x => x.TypeName, StringComparer.Ordinal)
        .ToList();

    private IReadOnlyList<ActivityDescriptor> VisibleDescriptors
    {
        get
        {
            IEnumerable<ActivityDescriptor> descriptors = string.IsNullOrWhiteSpace(_searchText)
                ? _selectedSection switch
                {
                    SuggestedSection => SuggestedDescriptors,
                    RecentSection => RecentDescriptors,
                    AllSection => _descriptors,
                    _ => _descriptors.Where(x => string.Equals(GetCategory(x), _selectedSection, StringComparison.OrdinalIgnoreCase))
                }
                : _descriptors.Where(MatchesSearch);

            return descriptors
                .OrderBy(GetDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.TypeName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private IEnumerable<IGrouping<string, ActivityDescriptor>> CategoryGroups => _descriptors
        .GroupBy(GetCategory, StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);

    private ActivityDescriptor? SequenceDescriptor =>
        _descriptors.FirstOrDefault(x => string.Equals(x.TypeName, "Elsa.Sequence", StringComparison.Ordinal));

    private string CommitLabel => (_selectedDescriptor, IsReplacing) switch
    {
        (null, true) => Localizer["Replace activity"],
        (null, false) => Localizer["Add activity"],
        ({ } descriptor, true) => Localizer["Replace with {0}", GetDisplayName(descriptor)],
        ({ } descriptor, false) => Localizer["Add {0}", GetDisplayName(descriptor)]
    };
    private string? SelectedResultId => _selectedDescriptor == null ? null : GetResultId(_selectedDescriptor);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ActivityRegistry.EnsureLoadedAsync();
            var allDescriptors = ActivityRegistry.List().ToList();
            var browsableDescriptors = ActivityRegistry.ListBrowsable().ToList();
            _rootActivityTypeNames = RootActivityTemplateProvider.List()
                .Select(x => x.Key)
                .ToHashSet(StringComparer.Ordinal);
            var designerBackedRoots = allDescriptors.Where(x => _rootActivityTypeNames.Contains(x.TypeName));

            var offeredDescriptors = DescriptorFilter != null
                ? allDescriptors.Where(DescriptorFilter)
                : browsableDescriptors.Concat(designerBackedRoots);

            _descriptors = offeredDescriptors
                .DistinctBy(x => x.TypeName, StringComparer.Ordinal)
                .OrderBy(GetDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.TypeName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _selectedSection = SuggestedDescriptors.Count > 0 ? SuggestedSection : AllSection;
            EnsureSelection();
            _focusSearch = true;
        }
        catch (Exception exception)
        {
            _loadError = exception;
        }
        finally
        {
            _isLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isLoading || _loadError != null)
            return;

        if (_focusSearch)
        {
            _focusSearch = false;
            await _searchInput.FocusAsync();
        }
    }

    private void OnSearchInput(ChangeEventArgs args)
    {
        _searchText = args.Value?.ToString() ?? string.Empty;
        EnsureSelection();
    }

    private void SelectSection(string section)
    {
        _selectedSection = section;
        _searchText = string.Empty;
        EnsureSelection();
    }

    private void Select(ActivityDescriptor descriptor) => _selectedDescriptor = descriptor;

    private Task SelectAndCommitAsync(ActivityDescriptor descriptor)
    {
        _selectedDescriptor = descriptor;
        return CommitAsync();
    }

    private Task CommitAsync()
    {
        if (_selectedDescriptor != null)
            MudDialog.Close(DialogResult.Ok(_selectedDescriptor));
        return Task.CompletedTask;
    }

    private Task HandleKeyboardAsync(KeyboardEventArgs args)
    {
        var results = VisibleDescriptors;
        if (results.Count == 0)
            return Task.CompletedTask;

        var currentIndex = _selectedDescriptor == null ? -1 : results.IndexOf(_selectedDescriptor);
        switch (args.Key)
        {
            case "ArrowDown":
                _selectedDescriptor = results[(currentIndex + 1 + results.Count) % results.Count];
                break;
            case "ArrowUp":
                _selectedDescriptor = results[(currentIndex - 1 + results.Count) % results.Count];
                break;
            case "Enter":
                return CommitAsync();
        }

        return Task.CompletedTask;
    }

    private Task HandleGlobalKeyboardAsync(KeyboardEventArgs args)
    {
        if (args.Key != "/")
            return Task.CompletedTask;

        _focusSearch = true;
        return InvokeAsync(StateHasChanged);
    }

    private void EnsureSelection()
    {
        var results = VisibleDescriptors;
        if (_selectedDescriptor == null || results.All(x => !ReferenceEquals(x, _selectedDescriptor)))
            _selectedDescriptor = results.FirstOrDefault();
    }

    private bool MatchesSearch(ActivityDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        var search = _searchText.Trim();
        return Contains(GetDisplayName(descriptor), search)
               || Contains(descriptor.Name, search)
               || Contains(descriptor.TypeName, search)
               || Contains(descriptor.Category, search)
               || Contains(descriptor.Description, search);
    }

    private static bool Contains(string? value, string search) =>
        value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private string GetDisplayName(ActivityDescriptor descriptor) => Localizer[descriptor.DisplayName ?? descriptor.Name].Value;
    private bool IsDesignerBackedRoot(ActivityDescriptor descriptor) => _rootActivityTypeNames.Contains(descriptor.TypeName);
    private string GetCategory(ActivityDescriptor descriptor) => string.IsNullOrWhiteSpace(descriptor.Category) ? Localizer["Other"] : Localizer[descriptor.Category];
    private string GetResultId(ActivityDescriptor descriptor) => $"{_id}-activity-{SanitizeId(descriptor.TypeName)}";
    private static string SanitizeId(string value) => new(value.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
    private bool IsSelected(ActivityDescriptor descriptor) => ReferenceEquals(_selectedDescriptor, descriptor);
    private bool IsSectionSelected(string section) => _selectedSection == section && string.IsNullOrWhiteSpace(_searchText);
    private string GetSectionClass(string section) => IsSectionSelected(section)
        ? "state-machine-activity-picker__filter state-machine-activity-picker__filter--selected"
        : "state-machine-activity-picker__filter";
    private void Cancel() => MudDialog.Cancel();
}

file static class ActivityDescriptorListExtensions
{
    public static int IndexOf(this IReadOnlyList<ActivityDescriptor> descriptors, ActivityDescriptor descriptor)
    {
        for (var index = 0; index < descriptors.Count; index++)
        {
            if (ReferenceEquals(descriptors[index], descriptor))
                return index;
        }

        return -1;
    }
}

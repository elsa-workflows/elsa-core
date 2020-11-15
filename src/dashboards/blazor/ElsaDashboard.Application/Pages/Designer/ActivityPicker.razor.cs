using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.Designer
{
    partial class ActivityPicker
    {
        [Parameter] public EventCallback<ActivityDescriptorSelectedEventArgs> ActivitySelected { get; set; }
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        private ActivityTraitFilter SelectedActivityTraitFilter { get; set; }
        private string? ActivitySearchText { get; set; }
        private ICollection<ActivityTraitFilter> ActivityTraitFilters => new[] { ActivityTraitFilter.All, ActivityTraitFilter.Actions, ActivityTraitFilter.Triggers };
        private ICollection<ActivityDescriptor> Activities { get; set; } = new System.Collections.Generic.List<ActivityDescriptor>();

        private ICollection<IGrouping<string, ActivityDescriptor>> ActivityGroupings =>
            FilterBySearchText(
                    FilterByTrait(Activities, SelectedActivityTraitFilter),
                    ActivitySearchText)
                .GroupBy(x => x.Category)
                .ToList();

        protected override async Task OnInitializedAsync()
        {
            var response = (await ActivityService.GetActivitiesAsync());
            Activities = response.Activities.ToList();
        }

        private void OnActivityTypeFilterClick(ActivityTraitFilter activityTraitFilter)
        {
            SelectedActivityTraitFilter = activityTraitFilter;
        }

        private static IEnumerable<ActivityDescriptor> FilterByTrait(IEnumerable<ActivityDescriptor> activities, ActivityTraitFilter filter) =>
            filter switch
            {
                ActivityTraitFilter.Actions => activities.Where(x => (x.Traits & ActivityTraits.Action) == ActivityTraits.Action),
                ActivityTraitFilter.Triggers => activities.Where(x => (x.Traits & ActivityTraits.Trigger) == ActivityTraits.Trigger),
                ActivityTraitFilter.All => activities,
                _ => activities
            };

        private static IEnumerable<ActivityDescriptor> FilterBySearchText(IEnumerable<ActivityDescriptor> activities, string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return activities;

            return
                from activity in activities
                where
                    (activity.Description ?? "").Contains(searchText, StringComparison.InvariantCultureIgnoreCase) ||
                    (activity.DisplayName ?? "").Contains(searchText, StringComparison.InvariantCultureIgnoreCase) ||
                    activity.Type.Contains(searchText, StringComparison.InvariantCultureIgnoreCase) ||
                    activity.Category.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)
                select activity;
        }

        private Task OnActivityClick(ActivityDescriptor activityDescriptor) => ActivitySelected.InvokeAsync(new ActivityDescriptorSelectedEventArgs(activityDescriptor));
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class ActivityPicker
    {
        [Parameter] public EventCallback<ActivityDescriptorSelectedEventArgs> ActivitySelected { get; set; }
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        private ActivityTraitFilter SelectedActivityTraitFilter { get; set; }
        private string? ActivitySearchText { get; set; }
        private ICollection<ActivityTraitFilter> ActivityTraitFilters => new[] { ActivityTraitFilter.All, ActivityTraitFilter.Actions, ActivityTraitFilter.Triggers };
        private ICollection<ActivityInfo> Activities { get; set; } = new List<ActivityInfo>();

        private ICollection<IGrouping<string, ActivityInfo>> ActivityGroupings =>
            FilterBySearchText(
                    FilterByTrait(Activities, SelectedActivityTraitFilter),
                    ActivitySearchText)
                .GroupBy(x => x.Category)
                .ToList();

        protected override async Task OnInitializedAsync()
        {
            var response = (await ActivityService.GetActivitiesAsync());
            Activities = response.ToList();
        }

        private void OnActivityTypeFilterClick(ActivityTraitFilter activityTraitFilter)
        {
            SelectedActivityTraitFilter = activityTraitFilter;
        }

        private static IEnumerable<ActivityInfo> FilterByTrait(IEnumerable<ActivityInfo> activities, ActivityTraitFilter filter) =>
            filter switch
            {
                ActivityTraitFilter.Actions => activities.Where(x => (x.Traits & ActivityTraits.Action) == ActivityTraits.Action),
                ActivityTraitFilter.Triggers => activities.Where(x => (x.Traits & ActivityTraits.Trigger) == ActivityTraits.Trigger),
                ActivityTraitFilter.All => activities,
                _ => activities
            };

        private static IEnumerable<ActivityInfo> FilterBySearchText(IEnumerable<ActivityInfo> activities, string? searchText)
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

        private Task OnActivityClick(ActivityInfo activityInfo) => ActivitySelected.InvokeAsync(new ActivityDescriptorSelectedEventArgs(activityInfo));
    }
}
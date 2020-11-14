using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Shared.Rpc;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Pages.Designer
{
    partial class ActivityPicker
    {
        [Inject] private IActivityService ActivityService { get; set; } = default!;
        private ICollection<IGrouping<string, ActivityDescriptor>> ActivityGroupings { get; set; } = new System.Collections.Generic.List<IGrouping<string, ActivityDescriptor>>();

        protected override async Task OnInitializedAsync()
        {
            var activities = (await ActivityService.GetActivitiesAsync()).ToList();
            ActivityGroupings = activities.GroupBy(x => x.Category).ToList();
        }
    }
}
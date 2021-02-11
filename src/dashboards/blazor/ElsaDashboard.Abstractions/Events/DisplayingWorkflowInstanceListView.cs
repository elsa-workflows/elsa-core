using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Events
{
    public class DisplayingWorkflowInstanceListView : INotification
    {
        public Func<IEnumerable<string>> SelectedWorkflowInstanceIds { get; }
        public Func<Task> Reload { get; }
        public ICollection<RenderFragment> BulkMenuItems { get; } = new List<RenderFragment>();

        public DisplayingWorkflowInstanceListView(Func<IEnumerable<string>> selectedWorkflowInstanceIds, Func<Task> reload)
        {
            SelectedWorkflowInstanceIds = selectedWorkflowInstanceIds;
            Reload = reload;
        }
    }
}
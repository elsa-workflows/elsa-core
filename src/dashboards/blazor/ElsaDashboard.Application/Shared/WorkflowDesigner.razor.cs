using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Formats.Asn1;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ElsaDashboard.Application.Extensions;
using ElsaDashboard.Application.Models;
using ElsaDashboard.Application.Services;
using ElsaDashboard.Models;
using ElsaDashboard.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ElsaDashboard.Application.Shared
{
    partial class WorkflowDesigner : IAsyncDisposable
    {
        private static Func<ConnectionModel, Task> _connectionCreatedAction = default!;

        [Parameter] public WorkflowModel Model { private get; set; } = WorkflowModel.Blank();
        [Parameter] public EventCallback<WorkflowModelChangedEventArgs> WorkflowChanged { get; set; }
        [Parameter] public EventCallback<DeleteActivityInvokedEventArgs> DeleteActivityInvoked { get; set; }
        [Parameter] public EventCallback<EditActivityInvokedEventArgs> EditActivityInvoked { get; set; }
        [Parameter] public EventCallback<AddActivityInvokedEventArgs> AddActivityInvoked { get; set; }
        [Parameter] public EventCallback<ActivitySelectedEventArgs> ActivitySelected { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        private IJSObjectReference _designerModule = default!;
        private bool _connectionsChanged = true;
        private EventCallbackFactory EventCallbackFactory { get; } = new();

        private readonly ActivityInfo _unknownActivityDescriptor =
            new()
            {
                Type = "Unknown",
                Category = "Unknown",
                Description = "Activity type not found",
                DisplayName = "Unknown"
            };

        [JSInvokableAttribute("InvokeConnectionCreated")]
        public static Task InvokeConnectionCreated(ConnectionModel connection) => _connectionCreatedAction(connection);

        protected override void OnInitialized()
        {
            _connectionCreatedAction = OnConnectionCreatedAsync;
        }

        protected override void OnParametersSet()
        {
            ConnectionsHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            if (_designerModule != null!)
                await _designerModule.DisposeAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_designerModule == null!)
                _designerModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/ElsaDashboard.Application/workflowDesigner.js");

            await RepaintConnections();
        }

        private async Task UpdateModelAsync(WorkflowModel model)
        {
            Model = model;
            ConnectionsHasChanged();

            await WorkflowChanged.InvokeAsync(new WorkflowModelChangedEventArgs(Model));
        }

        private IEnumerable<ActivityModel> GetRootActivities() => Model.GetChildActivities(null);

        private async ValueTask RepaintConnections()
        {
            if (!_connectionsChanged)
                return;

            _connectionsChanged = false;

            var connections = GetJsPlumbConnections();
            var sourceEndpoints = GetJsPlumbSourceEndpoints();
            var targets = GetJsPlumbTargets();
            await _designerModule.InvokeVoidAsync("updateConnections", (object) connections, (object) sourceEndpoints, (object) targets);
        }

        private IEnumerable<object> GetJsPlumbConnections()
        {
            var rootActivities = Model.GetChildActivities(null).ToList();

            var rootConnections = rootActivities.SelectMany(x =>
            {
                return new[]
                {
                    new
                    {
                        sourceId = "start-button",
                        sourceActivityId = default(string)!,
                        targetId = $"start-button-plus-{x.ActivityId}",
                        targetActivityId = x.ActivityId,
                        outcome = default(string)!
                    },
                    new
                    {
                        sourceId = $"start-button-plus-{x.ActivityId}",
                        sourceActivityId = default(string)!,
                        targetId = $"activity-{x.ActivityId}",
                        targetActivityId = x.ActivityId,
                        outcome = default(string)!
                    },
                };
            });

            var sourceConnections = Model.Activities.SelectMany(activity => activity.Outcomes.Select(x =>
                new
                {
                    sourceId = $"activity-{activity.ActivityId}",
                    sourceActivityId = activity.ActivityId,
                    targetId = $"{activity.ActivityId}-{x}",
                    targetActivityId = default(string)!,
                    outcome = x
                }));

            var connections = Model.Connections.SelectMany(x => new[]
            {
                new
                {
                    sourceId = $"{x.SourceId}-{x.Outcome}",
                    sourceActivityId = x.SourceId,
                    targetId = $"activity-{x.TargetId}",
                    targetActivityId = x.TargetId!,
                    outcome = x.Outcome
                },
            });

            return rootConnections.Concat(connections).Concat(sourceConnections);
        }

        private IEnumerable<object> GetJsPlumbSourceEndpoints()
        {
            var rootActivities = Model.GetChildActivities(null).ToList();

            var rootSourceEndpoints = rootActivities.Select(x => new
            {
                sourceId = $"start-button-plus-{x.ActivityId}",
                sourceActivityId = x.ActivityId,
                outcome = default(string)!
            });

            var otherSourceEndpoints = Model.Activities.SelectMany(activity => activity.Outcomes.Select(x => new
            {
                sourceId = $"{activity.ActivityId}-{x}",
                sourceActivityId = activity.ActivityId,
                outcome = x
            }));

            return rootSourceEndpoints.Concat(otherSourceEndpoints);
        }

        private IEnumerable<object> GetJsPlumbTargets() =>
            Model.Activities.Select(x => new
            {
                targetId = $"activity-{x.ActivityId}",
                targetActivityId = x.ActivityId
            });

        private RenderFragment RenderActivity(ActivityModel activityModel)
        {
            return builder =>
            {
                var index = 0;
                builder.OpenComponent<Activity>(index++);
                builder.AddAttribute(index++, nameof(Activity.Model), activityModel);
                //builder.AddAttribute(index++, nameof(Activity.OnEditClick), EventCallbackFactory.Create(this, () => OnActivityEdit(activityModel)));
                builder.AddAttribute(index++, nameof(Activity.OnDeleteClick), EventCallbackFactory.Create(this, () => OnActivityDelete(activityModel)));

                var displayDescriptor = activityModel.DisplayDescriptor;

                if (displayDescriptor != null)
                {
                    var context = new ActivityDisplayContext(activityModel.Type, activityModel.Properties);

                    if (displayDescriptor.RenderBody != null)
                        builder.AddAttribute(index++, nameof(Activity.Body), displayDescriptor.RenderBody(context));

                    if (displayDescriptor.RenderIcon != null)
                        builder.AddAttribute(index, nameof(Activity.Icon), displayDescriptor.RenderIcon(context));
                }

                builder.CloseComponent();
            };
        }

        private void ConnectionsHasChanged()
        {
            _connectionsChanged = true;
            StateHasChanged();
        }

        private async Task OnAddActivityClick(string? sourceActivityId, string? targetActivityId, string? outcome) => await AddActivityInvoked.InvokeAsync(new AddActivityInvokedEventArgs(sourceActivityId, targetActivityId, outcome));
        private async Task OnActivityDelete(ActivityModel activityModel) => await DeleteActivityInvoked.InvokeAsync(new DeleteActivityInvokedEventArgs(activityModel));

        //private async ValueTask OnActivityClick(MouseEventArgs e) => await FlyoutPanelService.ShowAsync<ActivityEditor>("Timer Properties");
        private async Task OnConnectionCreatedAsync(ConnectionModel connection) => await UpdateModelAsync(Model.AddConnection(connection));
    }
}
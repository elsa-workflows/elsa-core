using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ElsaDashboard.Application.Extensions;

namespace ElsaDashboard.Application.Models
{
    public sealed record WorkflowModel
    {
        public WorkflowModel()
        {
        }

        public WorkflowModel(string? name, IEnumerable<ActivityModel> activities, IEnumerable<ConnectionModel> connections) => (Name, Activities, Connections) = (name, activities.ToImmutableList(), connections.ToImmutableList());
        public WorkflowModel(string? name) => Name = (name);
        public string? Name { get; init; }
        public IImmutableList<ActivityModel> Activities { get; init; } = Array.Empty<ActivityModel>().ToImmutableList();
        public IImmutableList<ConnectionModel> Connections { get; init; } = Array.Empty<ConnectionModel>().ToImmutableList();

        public static WorkflowModel Blank() => new("New Workflow");
        public WorkflowModel AddActivity(ActivityModel activity) => this with { Activities = Activities.Add(activity) };
        public WorkflowModel AddConnection(ConnectionModel connection) => this with { Connections = Connections.Add(connection) };
        public WorkflowModel AddConnection(string sourceId, string targetId, string outcome) => AddConnection(new ConnectionModel(sourceId, targetId, outcome));
        public WorkflowModel RemoveConnection(ConnectionModel connection) => this with { Connections = Connections.Remove(connection) };
        public ActivityModel FindActivity(string activityId) => Activities.First(x => x.ActivityId == activityId);
        
        public WorkflowModel RemoveActivity(ActivityModel activity)
        {
            var inboundConnections = this.GetInboundConnections(activity.ActivityId);
            var outboundConnections = this.GetOutboundConnections(activity.ActivityId);
            var connectionsToRemove = inboundConnections.Concat(outboundConnections).ToList();
            
            return this with
            {
                Activities = Activities.Where(x => x != activity).ToImmutableList(),
                Connections = Connections.Where(x => !connectionsToRemove.Contains(x)).ToImmutableList()
            };
        }
    }
}
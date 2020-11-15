using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Elsa.Client.Models;

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

        public static WorkflowModel Demo() =>
            new WorkflowModel
            {
                Activities = new[]
                {
                    new ActivityModel("activity-1", "ReadLine", new[] { "Done" }),
                    new ActivityModel("activity-2", "IfElse", new[] { "True", "False" }),
                    new ActivityModel("activity-3", "WriteLine", new[] { "Done" }),
                    new ActivityModel("activity-4", "WriteLine", new[] { "Done" })
                }.ToImmutableList(),

                Connections = new[]
                {
                    new ConnectionModel("activity-1", "activity-2", "Done"),
                    new ConnectionModel("activity-2", "activity-3", "True"),
                    new ConnectionModel("activity-2", "activity-4", "False")
                }.ToImmutableList()
            };

        public WorkflowModel AddActivity(ActivityModel activity) => this with { Activities = Activities.Add(activity)};
        public WorkflowModel RemoveActivity(ActivityModel activity) => this with { Activities = Activities.Where(x => x != activity).ToImmutableList()};
        public WorkflowModel AddConnection(ConnectionModel connection) => this with { Connections = Connections.Add(connection)};
    }
}
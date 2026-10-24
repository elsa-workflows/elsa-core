using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Xunit;
using UserTask = Elsa.UserTasks.Activities.UserTask;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// The outcomes a completed <see cref="UserTask"/> reports: the default ones, which a flowchart's Done connection and a
/// connection naming no port follow, and the action a person chose, which a BPMN sequence flow can route on.
/// </summary>
public class UserTaskOutcomeTests
{
    [Theory]
    [InlineData("Approve", new[] { "Done", "Approve" })]
    [InlineData("Timeout", new[] { "Done", "Timeout" })]
    [InlineData("Cancelled", new[] { "Done", "Cancelled" })]
    [InlineData("Done", new[] { "Done" })]
    [InlineData("", new[] { "Done" })]
    [InlineData(null, new[] { "Done" })]
    public void A_completion_reports_the_default_outcomes_and_its_action(string? actionKey, string[] named)
    {
        Assert.Equal([null, .. named], UserTask.OutcomesOf(actionKey).Names);
    }

    [Theory]
    [InlineData("Approve")]
    [InlineData("Done")]
    [InlineData(null)]
    public void A_flowchart_follows_a_connection_that_names_no_port_and_the_Done_connection(string? actionKey)
    {
        // A connection built from two activities, as code-first flowcharts write it, leaves the source port null.
        var task = new UserTask("Approve order");
        var unnamed = new Connection(task, new WriteLine("unnamed"));
        var done = new Connection(new Endpoint(task, "Done"), new Endpoint(new WriteLine("done")));
        var reject = new Connection(new Endpoint(task, "Reject"), new Endpoint(new WriteLine("reject")));
        ICollection<Connection> connections = [unnamed, done, reject];

        Assert.Equal([unnamed, done], connections.OutboundConnections(task, UserTask.OutcomesOf(actionKey)));
    }
}

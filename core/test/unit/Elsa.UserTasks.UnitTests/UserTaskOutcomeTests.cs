using Xunit;
using UserTask = Elsa.UserTasks.Activities.UserTask;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// The outcomes a completed <see cref="UserTask"/> reports: <c>Done</c>, which a flowchart's Done connection follows,
/// and the action a person chose, which a BPMN sequence flow can route on.
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
    public void A_completion_reports_Done_and_its_action(string? actionKey, string[] expected)
    {
        Assert.Equal(expected, UserTask.OutcomesOf(actionKey));
    }
}

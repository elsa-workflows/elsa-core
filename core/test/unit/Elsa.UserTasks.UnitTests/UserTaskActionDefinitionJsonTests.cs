using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.UserTasks.Activities;
using Xunit;

namespace Elsa.UserTasks.UnitTests;

public class UserTaskActionDefinitionJsonTests
{
    [Fact]
    public void LiteralActionDefinitions_WithPlainStringLabels_CanBeConverted()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("""[{"key":"approved","label":"Approve"},{"key":"rejected","label":"Reject"}]""");

        var actions = ((IEnumerable<UserTaskActionDefinition>)json.ConvertTo(typeof(IReadOnlyCollection<UserTaskActionDefinition>))!).ToArray();

        Assert.Equal(["approved", "rejected"], actions.Select(x => x.Key));
        Assert.Equal(["Approve", "Reject"], actions.Select(x => x.Label.Expression!.Value as string));
    }

    [Fact]
    public void ActionDefinition_RoundTripsLiteralLabelAsPlainString()
    {
        var json = JsonSerializer.Serialize(new UserTaskActionDefinition("approved", "Approve"));

        var roundTripped = JsonSerializer.Deserialize<UserTaskActionDefinition>(json)!;

        Assert.Contains("\"Label\":\"Approve\"", json);
        Assert.Equal("Approve", roundTripped.Label.Expression!.Value);
    }
}

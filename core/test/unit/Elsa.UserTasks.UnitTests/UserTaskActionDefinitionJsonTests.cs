using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Models;
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

    [Theory]
    [InlineData("""{"key":"approved","label":{"typeName":"String","expression":{"type":"Literal","value":"Approve"}}}""")]
    [InlineData("""{"Key":"approved","Label":{"TypeName":"String","Expression":{"Type":"Literal","Value":"Approve"}}}""")]
    public void ActionDefinition_ReadsLegacyInputShapedLiteralLabel(string json)
    {
        var action = JsonSerializer.Deserialize<UserTaskActionDefinition>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.Equal("Approve", action.Label.Expression!.Value);
    }

    [Fact]
    public void ActionDefinition_ReadsLegacyInputShapedExpressionLabelAsNull()
    {
        const string json = """{"key":"approved","label":{"typeName":"String","expression":{"type":"JavaScript","value":"'Approve'"}}}""";

        var action = JsonSerializer.Deserialize<UserTaskActionDefinition>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.Null(action.Label);
    }

    [Fact]
    public void ActionDefinition_WritesExpressionLabelAsNull()
    {
        var action = new UserTaskActionDefinition
        {
            Key = "approved",
            Label = new Input<string>(new Expression("JavaScript", "'Approve'"))
        };

        var json = JsonSerializer.Serialize(action);

        Assert.Contains("\"Label\":null", json);
        Assert.Null(JsonSerializer.Deserialize<UserTaskActionDefinition>(json)!.Label);
    }
}

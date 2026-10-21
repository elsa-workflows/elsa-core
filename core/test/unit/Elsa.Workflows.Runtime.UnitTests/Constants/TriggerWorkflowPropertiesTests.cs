namespace Elsa.Workflows.Runtime.UnitTests.Constants;

public class TriggerWorkflowPropertiesTests
{
    [Fact(DisplayName = "WithStimulusHash replaces a caller property whose key differs from the reserved key only in case")]
    public void WithStimulusHash_ReplacesDifferentlyCasedReservedKey()
    {
        var properties = new Dictionary<string, object> { ["elsa:triggerstimulushash"] = "caller-value", ["Other"] = 1 };

        var result = TriggerWorkflowProperties.WithStimulusHash(properties, "matched-hash")!;

        // The runtime copies workflow properties into a case-insensitive dictionary, so a second, differently cased key would throw there.
        var runtimeView = new Dictionary<string, object>(result, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("matched-hash", runtimeView[TriggerWorkflowProperties.StimulusHash]);
        Assert.Equal(1, runtimeView["Other"]);
        Assert.Equal("caller-value", properties["elsa:triggerstimulushash"]);
    }
}

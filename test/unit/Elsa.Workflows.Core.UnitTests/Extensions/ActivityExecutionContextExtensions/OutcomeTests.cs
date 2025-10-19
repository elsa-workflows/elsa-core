using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class OutcomeTests
{

    [Theory]
    [InlineData(typeof(TestActivityWithPort), "Done")]
    [InlineData(typeof(TestActivityWithNamedPort), "CustomOutcome")]
    public async Task GetOutcomeName_ReturnsExpectedName(Type activityType, string expectedOutcomeName)
    {
        // Arrange
        var activity = (Activity)Activator.CreateInstance(activityType)!;
        var context = await CreateContextAsync(activity);

        // Act
        var outcomeName = context.GetOutcomeName("Done");

        // Assert
        Assert.Equal(expectedOutcomeName, outcomeName);
    }

    private class TestActivityWithPort : Activity
    {
        public IActivity? Done { get; set; }
    }

    private class TestActivityWithNamedPort : Activity
    {
        [Port("CustomOutcome")]
        public IActivity? Done { get; set; }
    }
}

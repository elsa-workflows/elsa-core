using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class OutcomeTests
{
    [Test]
    [Arguments(typeof(TestActivityWithPort), "Done")]
    [Arguments(typeof(TestActivityWithNamedPort), "CustomOutcome")]
    public async Task GetOutcomeName_ReturnsExpectedName(Type activityType, string expectedOutcomeName)
    {
        // Arrange
        var activity = (Activity)Activator.CreateInstance(activityType)!;
        var context = await CreateContextAsync(activity);

        // Act
        var outcomeName = context.GetOutcomeName("Done");

        // Assert
        await Assert.That(outcomeName).IsEqualTo(expectedOutcomeName);
    }

    public sealed class TestActivityWithPort : Activity
    {
        public IActivity? Done { get; set; }
    }

    public sealed class TestActivityWithNamedPort : Activity
    {
        [Port("CustomOutcome")]
        public IActivity? Done { get; set; }
    }
}
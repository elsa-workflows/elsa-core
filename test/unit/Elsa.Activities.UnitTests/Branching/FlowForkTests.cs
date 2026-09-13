using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Branching;

public class FlowForkTests
{
    [Test]
    [MethodDataSource(nameof(BranchTestCases))]
    public async Task Should_Complete_With_Specified_Branches(string[] branches, string[] expectedOutcomes)
    {
        // Arrange
        var flowFork = new FlowFork();
        if (branches.Length > 0)
            flowFork.Branches = new(branches);

        // Act
        var context = await ExecuteAsync(flowFork);

        // Assert - Activity should complete with all outcomes.
        var outcomes = context.GetOutcomes().ToList();

        await Assert.That(outcomes.Count).IsEqualTo(expectedOutcomes.Length);
        foreach (var expectedOutcome in expectedOutcomes)
            await Assert.That(outcomes).Contains(expectedOutcome);
    }

    public static IEnumerable<object[]> BranchTestCases()
    {
        yield return [Array.Empty<string>(), new[] { "Done" }];
        yield return [new[] { "SingleBranch" }, new[] { "SingleBranch" }];
        yield return [new[] { "Branch1", "Branch2", "Branch3" }, new[] { "Branch1", "Branch2", "Branch3" }];
    }

    private static Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return new ActivityTestFixture(activity).ExecuteAsync();
    }
}
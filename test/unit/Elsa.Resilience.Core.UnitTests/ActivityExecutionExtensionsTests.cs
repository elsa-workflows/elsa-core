using System.Text.Json.Nodes;
using Elsa.Resilience.Core.UnitTests.TestHelpers;
using Elsa.Resilience.Extensions;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Options;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class ActivityExecutionExtensionsTests
{
    [Test]
    [DisplayName("Retries-attempted flag should read as false before anything sets it")]
    public async Task GetRetriesAttemptedFlag_NeverSet_ReturnsFalse()
    {
        var context = await ContextFactory.CreateAsync(new WriteLine("test"));

        await Assert.That(context.GetRetriesAttemptedFlag()).IsFalse();
    }

    [Test]
    [DisplayName("Setting the retries-attempted flag should make it readable on the same context")]
    public async Task SetRetriesAttemptedFlag_SetsFlagOnContext()
    {
        var context = await ContextFactory.CreateAsync(new WriteLine("test"));

        context.SetRetriesAttemptedFlag();

        await Assert.That(context.GetRetriesAttemptedFlag()).IsTrue();
    }

    [Test]
    [DisplayName("Setting the retries-attempted flag should propagate all the way up the ancestor chain")]
    public async Task SetRetriesAttemptedFlag_PropagatesToAllAncestors()
    {
        var (root, middle, leaf) = await CreateThreeLevelChainAsync();

        leaf.SetRetriesAttemptedFlag();

        await Assert.That(leaf.GetRetriesAttemptedFlag()).IsTrue();
        await Assert.That(middle.GetRetriesAttemptedFlag()).IsTrue();
        await Assert.That(root.GetRetriesAttemptedFlag()).IsTrue();
    }

    [Test]
    [DisplayName("Setting the retries-attempted flag should not propagate down to descendants")]
    public async Task SetRetriesAttemptedFlag_DoesNotPropagateToDescendants()
    {
        var (root, middle, leaf) = await CreateThreeLevelChainAsync();

        middle.SetRetriesAttemptedFlag();

        await Assert.That(root.GetRetriesAttemptedFlag()).IsTrue();
        await Assert.That(middle.GetRetriesAttemptedFlag()).IsTrue();
        await Assert.That(leaf.GetRetriesAttemptedFlag()).IsFalse();
    }

    [Test]
    [DisplayName("Setting the resilience strategy should store the model as an activity property")]
    public async Task SetResilienceStrategy_StoresModelAsProperty()
    {
        var context = await ContextFactory.CreateAsync(new WriteLine("test"));
        var model = JsonNode.Parse("""{"$type":"TestRetryStrategy","id":"test-retry"}""")!;

        context.SetResilienceStrategy(model);

        var stored = context.GetProperty<JsonNode>("ResilienceStrategy");
        await Assert.That(stored).IsSameReferenceAs(model);
    }

    private static async Task<(ActivityExecutionContext Root, ActivityExecutionContext Middle, ActivityExecutionContext Leaf)> CreateThreeLevelChainAsync()
    {
        var leafActivity = new WriteLine("leaf");
        var middleActivity = new TestContainer
        {
            Activities = [leafActivity]
        };
        var rootActivity = new TestContainer
        {
            Activities = [middleActivity]
        };

        var root = await ContextFactory.CreateAsync(rootActivity);
        var workflowExecutionContext = root.WorkflowExecutionContext;
        var middle = await workflowExecutionContext.CreateActivityExecutionContextAsync(middleActivity, new ActivityInvocationOptions { Owner = root });
        var leaf = await workflowExecutionContext.CreateActivityExecutionContextAsync(leafActivity, new ActivityInvocationOptions { Owner = middle });

        return (root, middle, leaf);
    }
}

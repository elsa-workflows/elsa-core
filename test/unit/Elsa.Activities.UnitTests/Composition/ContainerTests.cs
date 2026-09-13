using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Composition;

/// <summary>
/// Unit tests for the <see cref="Container"/> activity focusing on ExecuteAsync behavior.
/// </summary>
public class ContainerTests
{
    [Test]
    [DisplayName("Container schedules child activities")]
    [Arguments(1)]
    [Arguments(3)]
    [Arguments(5)]
    public async Task Container_SchedulesChildActivities(int activityCount)
    {
        // Arrange
        var activities = CreateActivities(activityCount);
        var container = CreateContainerWithActivities(activities);

        // Act
        var context = await ExecuteContainerAsync(container);

        // Assert - First activity should be scheduled (sequential scheduling)
        await Assert.That(context.HasScheduledActivity(activities[0])).IsTrue();
    }

    [Test]
    [DisplayName("Container with no activities completes without scheduling")]
    public async Task Container_WithNoActivities_CompletesWithoutScheduling()
    {
        // Arrange
        var container = new TestContainer();

        // Act
        var context = await ExecuteContainerAsync(container);

        // Assert
        var scheduledActivities = context.WorkflowExecutionContext.Scheduler.List().ToList();
        await Assert.That(scheduledActivities).IsEmpty();
    }

    [Test]
    [DisplayName("Container declares variables in memory")]
    public async Task Container_DeclaresVariablesInMemory()
    {
        // Arrange
        var variable1 = new Variable<int>("Counter", 10);
        var variable2 = new Variable<string>("Name", "Test");
        var container = CreateContainerWithVariables(variable1, variable2);

        // Act
        var context = await ExecuteContainerAsync(container);

        // Assert
        var memory = context.ExpressionExecutionContext.Memory;
        await Assert.That(memory.HasBlock(variable1.Id)).IsTrue();
        await Assert.That(memory.HasBlock(variable2.Id)).IsTrue();
    }

    [Test]
    [DisplayName("Container auto-names unnamed variables")]
    public async Task Container_AutoNamesUnnamedVariables()
    {
        // Arrange
        var var1 = new Variable<int>();
        var var2 = new Variable<string>();
        var var3 = new Variable<bool>();
        var container = CreateContainerWithVariables(var1, var2, var3);

        // Act
        await ExecuteContainerAsync(container);

        // Assert
        await Assert.That(var1.Name).IsEqualTo("Variable1");
        await Assert.That(var2.Name).IsEqualTo("Variable2");
        await Assert.That(var3.Name).IsEqualTo("Variable3");
    }

    [Test]
    [DisplayName("Container preserves named variables")]
    public async Task Container_PreservesNamedVariables()
    {
        // Arrange
        var namedVar = new Variable<string>("MyVariable", "value");
        var unnamedVar = new Variable<int>();
        var container = CreateContainerWithVariables(namedVar, unnamedVar);

        // Act
        await ExecuteContainerAsync(container);

        // Assert
        await Assert.That(namedVar.Name).IsEqualTo("MyVariable");
        await Assert.That(unnamedVar.Name).IsEqualTo("Variable1");
    }

    [Test]
    [DisplayName("Container handles multiple unnamed variables correctly")]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task Container_HandlesMultipleUnnamedVariables(int variableCount)
    {
        // Arrange
        var variables = CreateUnnamedVariables(variableCount);
        var container = CreateContainerWithVariables(variables.Cast<Variable>().ToArray());

        // Act
        await ExecuteContainerAsync(container);

        // Assert
        for (var i = 0; i < variableCount; i++)
        {
            await Assert.That(variables[i].Name).IsEqualTo($"Variable{i + 1}");
        }
    }

    [Test]
    [DisplayName("Container with mixed named and unnamed variables")]
    public async Task Container_WithMixedNamedAndUnnamedVariables()
    {
        // Arrange
        var namedVar1 = new Variable<int>("FirstVar", 1);
        var unnamedVar1 = new Variable<int>();
        var namedVar2 = new Variable<string>("SecondVar", "test");
        var unnamedVar2 = new Variable<bool>();
        var container = CreateContainerWithVariables(namedVar1, unnamedVar1, namedVar2, unnamedVar2);

        // Act
        await ExecuteContainerAsync(container);

        // Assert
        await Assert.That(namedVar1.Name).IsEqualTo("FirstVar");
        await Assert.That(unnamedVar1.Name).IsEqualTo("Variable1");
        await Assert.That(namedVar2.Name).IsEqualTo("SecondVar");
        await Assert.That(unnamedVar2.Name).IsEqualTo("Variable2");
    }

    [Test]
    [DisplayName("Container schedules mixed activity types")]
    public async Task Container_SchedulesMixedActivityTypes()
    {
        // Arrange
        var writeLine = new WriteLine("Test output");
        var testVar = new Variable<int>("testVar", 0, "testVar");
        var setVariable = new SetVariable<int>(testVar, new Input<int>(42));
        var mockActivity = Substitute.For<IActivity>();
        var container = CreateContainerWithActivities(writeLine, setVariable, mockActivity);

        // Act
        var context = await ExecuteContainerAsync(container);

        // Assert
        await Assert.That(context.HasScheduledActivity(writeLine)).IsTrue();
    }

    [Test]
    [DisplayName("Container with single activity schedules it")]
    public async Task Container_WithSingleActivity_SchedulesIt()
    {
        // Arrange
        var activity = new WriteLine("Single activity");
        var container = CreateContainerWithActivities(activity);

        // Act
        var context = await ExecuteContainerAsync(container);

        // Assert
        await Assert.That(context.HasScheduledActivity(activity)).IsTrue();
    }

    [Test]
    [DisplayName("Container with variables and activities works correctly")]
    public async Task Container_WithVariablesAndActivities_WorksCorrectly()
    {
        // Arrange
        var variable = new Variable<int>("Counter", 0);
        var activity = new WriteLine("Activity");
        var container = new TestContainer();
        container.Variables.Add(variable);
        container.Activities.Add(activity);

        // Act
        var context = await ExecuteContainerAsync(container);

        // Assert
        await Assert.That(context.ExpressionExecutionContext.Memory.HasBlock(variable.Id)).IsTrue();
        await Assert.That(context.HasScheduledActivity(activity)).IsTrue();
    }

    private static Task<ActivityExecutionContext> ExecuteContainerAsync(TestContainer container) =>
        new ActivityTestFixture(container).ExecuteAsync();

    private static IActivity[] CreateActivities(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new WriteLine($"Activity {i}"))
            .Cast<IActivity>()
            .ToArray();

    private static List<Variable<int>> CreateUnnamedVariables(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => new Variable<int>())
            .ToList();

    private static TestContainer CreateContainerWithActivities(params IActivity[] activities)
    {
        var container = new TestContainer();
        foreach (var activity in activities)
        {
            container.Activities.Add(activity);
        }
        return container;
    }

    private static TestContainer CreateContainerWithVariables(params Variable[] variables)
    {
        var container = new TestContainer();
        foreach (var variable in variables)
        {
            container.Variables.Add(variable);
        }
        return container;
    }
}
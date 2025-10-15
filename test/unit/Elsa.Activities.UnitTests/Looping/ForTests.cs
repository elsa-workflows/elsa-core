using Elsa.Activities.UnitTests.Helpers;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Behaviors;

namespace Elsa.Activities.UnitTests.Looping;

public class ForTests
{
    [Theory]
    [InlineData(1, 3, 1, 3, new[] { 1, 2, 3 }, 3)] // Ascending loop
    [InlineData(5, 1, -1, 5, new[] { 5, 4, 3, 2, 1 }, 1)] // Descending loop
    [InlineData(10, 12, 1, 3, new[] { 10, 11, 12 }, 12)] // UpdatesCurrentValueEachIteration
    [InlineData(0, 4, 1, 5, new[] { 0, 1, 2, 3, 4 }, 4)] // HandlesFloatingPointSteps equivalent
    [InlineData(10, 15, 1, 6, new[] { 10, 11, 12, 13, 14, 15 }, 15)] // PreservesIterationCountAccuracy
    [InlineData(-1, -5, -1, 5, new[] { -1, -2, -3, -4, -5 }, -5)] // NegativeRangeAndNegativeStep
    public async Task ExecutesValidLoopConfigurations(int start, int end, int step, int expectedCount, int[] expectedValues, int expectedFinalValue)
    {
        // Arrange
        var mockBody = CreateMockBodyActivity();
        var forActivity = new For(start, end, step) { Body = mockBody };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert
        AssertLoopExecution(mockBody, expectedCount, expectedValues);
        Assert.Equal(expectedFinalValue, context.GetExecutionOutput(_ => forActivity.CurrentValue));
    }

    [Theory]
    [InlineData(1, 3, true, 3, new[] { 1, 2, 3 })] // Inclusive bounds
    [InlineData(1, 3, false, 2, new[] { 1, 2 })] // Exclusive bounds
    [InlineData(5, 5, true, 1, new[] { 5 })] // Empty range inclusive
    [InlineData(5, 5, false, 0, new int[0])] // Empty range exclusive
    public async Task ExecutesBoundaryConditions(int start, int end, bool inclusive, int expectedCount, int[] expectedValues)
    {
        // Arrange
        var mockBody = CreateMockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(1),
            OuterBoundInclusive = new Input<bool>(inclusive),
            Body = mockBody
        };

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert
        AssertLoopExecution(mockBody, expectedCount, expectedValues);
    }

    [Theory]
    [InlineData(1, 5, 0)] // Zero step
    [InlineData(5, 1, 1)] // Positive step with descending range
    [InlineData(1, 5, -1)] // Negative step with ascending range
    public async Task SkipsLoopWhenInvalidConfiguration(int start, int end, int step)
    {
        // Arrange
        var mockBody = CreateMockBodyActivity();
        var forActivity = new For(start, end, step) { Body = mockBody };

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert
        Assert.Equal(0, mockBody.ExecutionCount);
        Assert.Empty(mockBody.ReceivedValues);
    }

    [Fact]
    public async Task BodyIsNull_CompletesImmediately()
    {
        // Arrange
        var forActivity = new For(1, 5, 1) { Body = null };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert - Should complete without errors
        Assert.True(context.IsCompleted);
    }

    [Fact]
    public async Task BreakBehavior_StopsLoopEarly()
    {
        // Arrange
        var mockBody = CreateMockBodyActivity();
        mockBody.ShouldBreak = true;
        var forActivity = new For(1, 10, 1) { Body = mockBody };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert - Should only execute twice (break on second iteration)
        AssertLoopExecution(mockBody, 2, new[] { 1, 2 });
    }

    [Fact]
    public async Task CurrentValueOutputTypeCheck_PreservesIntegerType()
    {
        // Arrange
        var mockBody = CreateMockBodyActivity();
        var forActivity = new For(1, 3, 1) { Body = mockBody };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert
        var currentValue = context.GetExecutionOutput(_ => forActivity.CurrentValue);
        Assert.IsType<int>(currentValue);
        Assert.Equal(3, currentValue);
    }

    [Fact]
    public async Task LoopVariableExposureToChildContext()
    {
        // Arrange
        var mockBody = CreateMockBodyActivity();
        var forActivity = new For(100, 102, 1) { Body = mockBody };

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert - Mock body should have received the CurrentValue variable correctly
        AssertLoopExecution(mockBody, 3, new[] { 100, 101, 102 });
    }

    [Fact]
    public async Task LargeIterationRangeSafety()
    {
        // Arrange - Test with a reasonably large range
        var mockBody = CreateMockBodyActivity();
        var forActivity = new For(1, 1000, 1) { Body = mockBody };

        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert - Should complete without overflow or performance issues
        Assert.Equal(1000, mockBody.ExecutionCount);
        Assert.Equal(1, mockBody.ReceivedValues.First());
        Assert.Equal(1000, mockBody.ReceivedValues.Last());
        Assert.True(context.IsCompleted);
    }

    [Fact]
    public async Task IdempotentExecution()
    {
        // Arrange
        var mockBody1 = CreateMockBodyActivity();
        var mockBody2 = CreateMockBodyActivity();
        var forActivity1 = new For(1, 3, 1) { Body = mockBody1 };
        var forActivity2 = new For(1, 3, 1) { Body = mockBody2 };

        // Act - Execute same configuration twice
        await ActivityTestHelper.ExecuteActivityAsync(forActivity1);
        await ActivityTestHelper.ExecuteActivityAsync(forActivity2);

        // Assert - Both should yield identical results
        Assert.Equal(mockBody1.ExecutionCount, mockBody2.ExecutionCount);
        Assert.Equal(mockBody1.ReceivedValues, mockBody2.ReceivedValues);
    }

    [Theory]
    [InlineData(0, 5, 6, 0)] // Start with default value (0)
    [InlineData(1, 0, 0, 0)] // End with explicit zero value
    public async Task HandlesDefaultValues(int start, int end, int expectedCount, int expectedFirstValue)
    {
        // Arrange
        var mockBody = CreateMockBodyActivity();
        var forActivity = new For
        {
            Start = new Input<int>(start),
            End = new Input<int>(end),
            Step = new Input<int>(1),
            Body = mockBody
        };

        // Act
        await ActivityTestHelper.ExecuteActivityAsync(forActivity);

        // Assert
        Assert.Equal(expectedCount, mockBody.ExecutionCount);
        if (expectedCount > 0)
        {
            Assert.Equal(expectedFirstValue, mockBody.ReceivedValues.First());
        }
    }

    [Fact]
    public void VerifyActivityAttributes()
    {
        // Act & Assert
        ActivityTestHelper.AssertActivityAttributes(
            typeof(For),
            expectedNamespace: "Elsa",
            expectedCategory: "Looping", 
            expectedDisplayName: "For",
            expectedDescription: "Iterate over a sequence of steps between a start and an end number.",
            expectedKind: ActivityKind.Action
        );
    }

    [Fact]
    public void VerifyBreakBehaviorIsRegistered()
    {
        // Arrange
        var forActivity = new For();

        // Act & Assert - Verify BreakBehavior is added to the activity
        var breakBehavior = forActivity.Behaviors.OfType<BreakBehavior>().FirstOrDefault();
        Assert.NotNull(breakBehavior);
    }

    [Fact]
    public void DefaultPropertyValues()
    {
        // Arrange
        var forActivity = new For();

        // Act & Assert - Verify default input values are properly initialized
        Assert.NotNull(forActivity.Start);
        Assert.NotNull(forActivity.End);
        Assert.NotNull(forActivity.Step);
        Assert.NotNull(forActivity.OuterBoundInclusive);
    }

    // Private helper methods (moved after all public members)
    private static MockBodyActivity CreateMockBodyActivity() => new();

    private static void AssertLoopExecution(MockBodyActivity mockBody, int expectedCount, int[] expectedValues)
    {
        Assert.Equal(expectedCount, mockBody.ExecutionCount);
        Assert.Equal(expectedValues.Cast<object>(), mockBody.ReceivedValues);
    }

    /// <summary>
    /// Mock activity to track execution count and receive loop variables
    /// </summary>
    private class MockBodyActivity : Activity
    {
        public int ExecutionCount { get; private set; }
        public List<object?> ReceivedValues { get; } = new();
        public bool ShouldBreak { get; set; }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            ExecutionCount++;
            
            // Capture the CurrentValue variable that should be available in the context
            // The For activity passes variables through the ScheduleActivityAsync call
            var currentValue = context.ExpressionExecutionContext.GetVariable<int>("CurrentValue");
            ReceivedValues.Add(currentValue);

            if (ShouldBreak && ExecutionCount == 2) // Break on second iteration
            {
                context.SetIsBreaking();
            }

            await context.CompleteActivityAsync();
        }
    }
}

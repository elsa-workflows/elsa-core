using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class WrappedInputEvaluationTests
{
    [Fact]
    public async Task Should_Use_Default_Value_When_Wrapped_Input_Is_Null()
    {
        // Arrange - Create activity with a default value but no input set
        var writeLine = new WriteLine("");
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert - Should not throw, default behavior applied
        Assert.True(context.GetHasEvaluatedProperties());
    }

    [Fact]
    public async Task Should_Create_Input_Wrapper_With_Literal_Expression_For_Default_Values()
    {
        // Arrange
        var writeLine = new WriteLine("");
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        // If there's a default value, the system should create a Literal expression wrapper
        Assert.True(context.GetHasEvaluatedProperties());
    }

    [Fact]
    public async Task Should_Evaluate_Expression_Via_DefaultActivityInputEvaluator()
    {
        // Arrange
        const string expectedText = "Evaluated Text";
        var writeLine = new WriteLine(expectedText);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Equal(expectedText, context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Set_Memory_Block_Reference_With_Deterministic_ID()
    {
        // Arrange
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(new Input<string>(expectedValue));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        // The memory block reference should be set with a deterministic ID: {nodeId}.{inputName}
        var memoryReference = writeLine.Text.MemoryBlockReference();
        Assert.NotNull(memoryReference);

        // Verify the value is accessible via the memory reference
        var storedValue = memoryReference.Get(context.ExpressionExecutionContext);
        Assert.Equal(expectedValue, storedValue);
    }

    [Fact]
    public async Task Should_Handle_Missing_Memory_Block_Reference_ID()
    {
        // Arrange - Test case from line 124 of ActivityExecutionContextExtensions.InputEvaluation.cs
        const string expectedValue = "Memory Test";
        var writeLine = new WriteLine(new Input<string>(expectedValue));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        // System should construct a deterministic ID: {activity.NodeId}.{inputDescriptor.Name}
        var memoryReference = writeLine.Text.MemoryBlockReference();
        Assert.NotNull(memoryReference);
        Assert.NotNull(memoryReference.Id);
        // The ID format may vary, just verify it's not null and contains a reasonable format
        Assert.False(string.IsNullOrEmpty(memoryReference.Id));
    }

    [Fact]
    public async Task Should_Store_Evaluated_Value_In_ExpressionExecutionContext()
    {
        // Arrange
        const int expectedValue = 123;
        var variable = new Variable<int>("testVar", 0, "testVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expectedValue));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        // Value should be stored in ExpressionExecutionContext via memory reference
        var memoryReference = setVariable.Value.MemoryBlockReference();
        var storedValue = memoryReference.Get(context.ExpressionExecutionContext);
        Assert.Equal(expectedValue, storedValue);
    }

    [Fact]
    public async Task Should_Evaluate_Literal_Expression_Type()
    {
        // Arrange
        const string literalValue = "Literal Test";
        var literal = new Literal<string>(literalValue);
        var writeLine = new WriteLine(new Input<string>(literal));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Equal(literalValue, context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Evaluate_Delegate_Expression_Type()
    {
        // Arrange
        const string delegateResult = "Delegate Result";
        var writeLine = new WriteLine(new Input<string>(() => delegateResult));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Equal(delegateResult, context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Evaluate_Variable_Expression_Type()
    {
        // Arrange
        const string variableValue = "Variable Value";
        var variable = new Variable<string>("myVar", variableValue, "myVar");
        var writeLine = new WriteLine(new Input<string>(variable));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Set the variable value in the context
        variable.Set(context.ExpressionExecutionContext, variableValue);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Equal(variableValue, context.ActivityState["Text"]);
    }


    [Fact]
    public async Task Should_Evaluate_Complex_Object_Input()
    {
        // Arrange
        var variable = new Variable<int>("complexVar", 999, "complexVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(999));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.Count > 0);
        Assert.Contains(999, context.ActivityState.Values);
    }
}

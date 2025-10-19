using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class WorkflowInputTests
{

    [Theory]
    [InlineData("TestKey", "TestValue", true, "TestValue", null)] // Key exists, string value
    [InlineData("NonExistentKey", null, false, null, null)] // Key doesn't exist
    [InlineData("NumberKey", null, true, "42", 42)] // Type conversion from int to string
    public async Task TryGetWorkflowInput_HandlesVariousScenarios(
        string key,
        string? stringValue,
        bool expectedResult,
        string? expectedValue,
        object? inputValue)
    {
        // Arrange
        var context = await CreateContextAsync();
        if (stringValue != null)
            context.WorkflowExecutionContext.Input[key] = stringValue;
        else if (inputValue != null)
            context.WorkflowExecutionContext.Input[key] = inputValue;

        // Act
        var result = context.TryGetWorkflowInput<string>(key, out var value);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public async Task GetWorkflowInput_ReturnsValue_WhenKeyExists()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.WorkflowExecutionContext.Input["TestKey"] = "TestValue";

        // Act
        var value = context.GetWorkflowInput<string>("TestKey");

        // Assert
        Assert.Equal("TestValue", value);
    }

    [Fact]
    public async Task GetWorkflowInput_UsesTypeName_WhenKeyNotProvided()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.WorkflowExecutionContext.Input["String"] = "TestValue";

        // Act
        var value = context.GetWorkflowInput<string>();

        // Assert
        Assert.Equal("TestValue", value);
    }

    [Fact]
    public async Task GetWorkflowInput_ThrowsException_WhenKeyDoesNotExist()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => context.GetWorkflowInput<string>("NonExistentKey"));
    }
}

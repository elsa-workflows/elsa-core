using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class WorkflowInputTests
{
    [Test]
    [Arguments("TestKey", "TestValue", true, "TestValue", null)] // Key exists, string value
    [Arguments("NonExistentKey", null, false, null, null)] // Key doesn't exist
    [Arguments("NumberKey", null, true, "42", 42)] // Type conversion from int to string
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
        await Assert.That(result).IsEqualTo(expectedResult);
        await Assert.That(value).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task GetWorkflowInput_ReturnsValue_WhenKeyExists()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.WorkflowExecutionContext.Input["TestKey"] = "TestValue";

        // Act
        var value = context.GetWorkflowInput<string>("TestKey");

        // Assert
        await Assert.That(value).IsEqualTo("TestValue");
    }

    [Test]
    public async Task GetWorkflowInput_UsesTypeName_WhenKeyNotProvided()
    {
        // Arrange
        var context = await CreateContextAsync();
        context.WorkflowExecutionContext.Input["String"] = "TestValue";

        // Act
        var value = context.GetWorkflowInput<string>();

        // Assert
        await Assert.That(value).IsEqualTo("TestValue");
    }

    [Test]
    public async Task GetWorkflowInput_ThrowsException_WhenKeyDoesNotExist()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act & Assert
        Assert.ThrowsExactly<KeyNotFoundException>(() => context.GetWorkflowInput<string>("NonExistentKey"));
    }
}
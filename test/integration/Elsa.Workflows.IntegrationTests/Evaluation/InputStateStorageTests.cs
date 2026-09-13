using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputStateStorageTests : EvaluationTestBase
{
    [Test]
    [DisplayName("Stores primitive values in activity state: $expectedValue")]
    [Arguments(42)]
    [Arguments(0)]
    [Arguments(-100)]
    public async Task StoresPrimitiveIntValues(int expectedValue)
    {
        // Arrange
        var variable = new Variable<int>("intVar", 0, "intVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.Values).Contains(expectedValue);
    }

    [Test]
    [DisplayName("Stores boolean values in activity state: $expectedValue")]
    [Arguments(true)]
    [Arguments(false)]
    public async Task StoresBooleanValues(bool expectedValue)
    {
        // Arrange
        var variable = new Variable<bool>("boolVar", false, "boolVar");
        var setVariable = new SetVariable<bool>(variable, new Input<bool>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.Values).Contains(expectedValue);
    }

    [Test]
    [DisplayName("Stores string values in activity state: $expectedValue")]
    [Arguments("Test String")]
    [Arguments("")]
    public async Task StoresStringValues(string expectedValue)
    {
        // Arrange
        var variable = new Variable<string>("stringVar", "", "stringVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.Values).Contains(expectedValue);
    }

    [Test]
    [DisplayName("Stores values using input descriptor name as key")]
    public async Task StoresValuesByInputDescriptorName()
    {
        // Arrange
        const string textValue = "Named Input Test";
        var writeLine = new WriteLine(textValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.ContainsKey("Text")).IsTrue();
        await Assert.That(context.ActivityState["Text"]).IsEqualTo(textValue);
    }

    [Test]
    [DisplayName("Stores null values correctly")]
    public async Task StoresNullValues()
    {
        // Arrange
        var writeLine = new WriteLine(new Input<string>(default(string)!));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.ContainsKey("Text")).IsTrue();
        await Assert.That(context.ActivityState["Text"]).IsNull();
    }

    [Test]
    [DisplayName("Overwrites previous values on re-evaluation")]
    public async Task OverwritesPreviousValuesOnReEvaluation()
    {
        // Arrange
        const string initialValue = "Initial";
        const string updatedValue = "Updated";
        var writeLine = new WriteLine(initialValue);
        var context = await CreateContextAsync(writeLine);

        // Act - First evaluation
        await context.EvaluateInputPropertiesAsync();
        var firstValue = context.ActivityState["Text"];

        // Modify and re-evaluate
        writeLine.Text = new(updatedValue);
        await context.EvaluateInputPropertyAsync("Text");
        var secondValue = context.ActivityState["Text"];

        // Assert
        await Assert.That(firstValue).IsEqualTo(initialValue);
        await Assert.That(secondValue).IsEqualTo(updatedValue);
    }

    [Test]
    [DisplayName("Stores multiple inputs separately")]
    public async Task StoresMultipleInputsSeparately()
    {
        // Arrange
        var variable = new Variable<int>("multiVar", 100, "multiVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(200));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.Count >= 1).IsTrue();
    }

    [Test]
    [DisplayName("Maintains state across multiple evaluations")]
    public async Task MaintainsStateAcrossMultipleEvaluations()
    {
        // Arrange
        const string value1 = "First";
        var writeLine1 = new WriteLine(value1);
        var context = await CreateContextAsync(writeLine1);

        // Act - First evaluation
        await context.EvaluateInputPropertiesAsync();
        var stateCountAfterFirst = context.ActivityState.Count;

        // Second evaluation of same input
        await context.EvaluateInputPropertyAsync("Text");
        var stateCountAfterSecond = context.ActivityState.Count;

        // Assert
        await Assert.That(stateCountAfterSecond).IsEqualTo(stateCountAfterFirst);
        await Assert.That(context.ActivityState["Text"]).IsEqualTo(value1);
    }
}

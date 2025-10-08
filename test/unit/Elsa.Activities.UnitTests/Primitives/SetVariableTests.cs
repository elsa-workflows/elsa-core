using Elsa.Activities.UnitTests.Helpers;
using Elsa.Workflows;
using Elsa.Expressions.Models;
using System.Text.Json;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetVariableTests
{
    [Fact]
    public async Task Should_Set_Variable_Integer() 
    {
        // Arrange
        var variable = new Variable<int>("myVar", 0, "myVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(42, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Should_Set_Variable_String()
    {
        // Arrange
        var variable = new Variable<string>("myStringVar", "", "myStringVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Hello World", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task Should_Overwrite_Existing_Variable()
    {
        // Arrange
        var variable = new Variable<int>("existingVar", 100, "inputId");
        var setVariable = new SetVariable<int>(variable, new Input<int>(200, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert - verify the original value was overwritten
        var result = variable.Get(context);
        Assert.Equal(200, result);
    }

    [Fact]
    public async Task Should_Assign_Null_Value()
    {
        // Arrange
        var variable = new Variable<string>("nullVar", "initial", "nullVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>((string)null!, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Set_Variable_With_Special_Characters()
    {
        // Arrange
        var variable = new Variable<string>("special_var-123", "", "specialVar");
        const string specialValue = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";
        var setVariable = new SetVariable<string>(variable, new Input<string>(specialValue, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(specialValue, result);
    }

    [Fact]
    public async Task Should_Serialize_Complex_Object()
    {
        // Arrange
        var complexObject = new ComplexTestObject
        {
            Id = 123,
            Name = "Test Object",
            Properties = new Dictionary<string, object>
            {
                {"prop1", "value1"},
                {"prop2", 456},
                {"prop3", true}
            },
            Items = ["item1", "item2", "item3"]
        };
        
        var variable = new Variable<ComplexTestObject>("complexVar", null!, "complexVar");
        var setVariable = new SetVariable<ComplexTestObject>(variable, new Input<ComplexTestObject>(complexObject, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.NotNull(result);
        Assert.Equal(complexObject.Id, result.Id);
        Assert.Equal(complexObject.Name, result.Name);
        Assert.Equal(complexObject.Properties.Count, result.Properties.Count);
        Assert.Equal(complexObject.Items.Length, result.Items.Length);
    }

    [Fact]
    public async Task Should_Assign_Large_Payload()
    {
        // Arrange
        var largePayload = new string('A', 1024 * 1024);
        var variable = new Variable<string>("largeVar", "", "largeVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>(largePayload, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(largePayload.Length, result.Length);
        Assert.Equal(largePayload, result);
    }

    [Fact]
    public async Task Should_Handle_Case_Sensitivity_On_Variable_Names()
    {
        // Arrange
        var variable1 = new Variable<string>("CaseSensitive", "value1", "var1");
        var variable2 = new Variable<string>("casesensitive", "value2", "var2");
        
        var setVariable1 = new SetVariable<string>(variable1, new Input<string>("updated1", "input1"));
        var setVariable2 = new SetVariable<string>(variable2, new Input<string>("updated2", "input2"));
        
        // Act
        var context1 = await ActivityTestHelper.ExecuteActivityAsync(setVariable1);
        var context2 = await ActivityTestHelper.ExecuteActivityAsync(setVariable2);
        
        // Assert - Variables with different casing should be treated as separate
        var result1 = variable1.Get(context1);
        var result2 = variable2.Get(context2);
        
        Assert.Equal("updated1", result1);
        Assert.Equal("updated2", result2);
    }

    [Fact]
    public async Task Should_Reassign_Variable_Multiple_Times()
    {
        // Arrange
        var variable = new Variable<int>("multiVar", 0, "multiVar");
        
        // Act - Multiple assignments
        var setVariable1 = new SetVariable<int>(variable, new Input<int>(10, "input1"));
        var context1 = await ActivityTestHelper.ExecuteActivityAsync(setVariable1);
        var result1 = variable.Get(context1);
        
        var setVariable2 = new SetVariable<int>(variable, new Input<int>(20, "input2"));
        var context2 = await ActivityTestHelper.ExecuteActivityAsync(setVariable2);
        var result2 = variable.Get(context2);
        
        var setVariable3 = new SetVariable<int>(variable, new Input<int>(30, "input3"));
        var context3 = await ActivityTestHelper.ExecuteActivityAsync(setVariable3);
        var result3 = variable.Get(context3);
        
        // Assert - Each execution should update the variable
        Assert.Equal(10, result1);
        Assert.Equal(20, result2);
        Assert.Equal(30, result3);
    }

    [Fact]
    public async Task Should_Handle_Boolean_Values()
    {
        // Arrange
        var variable = new Variable<bool>("boolVar", false, "boolVar");
        var setVariable = new SetVariable<bool>(variable, new Input<bool>(true, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.True(result);
    }

    [Fact]
    public async Task Should_Handle_DateTime_Values()
    {
        // Arrange
        var testDate = new DateTime(2025, 10, 8, 14, 30, 0);
        var variable = new Variable<DateTime>("dateVar", DateTime.MinValue, "dateVar");
        var setVariable = new SetVariable<DateTime>(variable, new Input<DateTime>(testDate, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(testDate, result);
    }

    [Fact]
    public async Task Should_Handle_Decimal_Values()
    {
        // Arrange
        var decimalValue = 123.456789m;
        var variable = new Variable<decimal>("decimalVar", 0m, "decimalVar");
        var setVariable = new SetVariable<decimal>(variable, new Input<decimal>(decimalValue, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(decimalValue, result);
    }

    [Fact]
    public async Task Should_Handle_Array_Values()
    {
        // Arrange
        var arrayValue = new[] { "item1", "item2", "item3" };
        var variable = new Variable<string[]>("arrayVar", null!, "arrayVar");
        var setVariable = new SetVariable<string[]>(variable, new Input<string[]>(arrayValue, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.NotNull(result);
        Assert.Equal(arrayValue.Length, result.Length);
        Assert.Equal(arrayValue, result);
    }

    [Fact]
    public async Task Should_Handle_Dictionary_Values()
    {
        // Arrange
        var dictionaryValue = new Dictionary<string, object>
        {
            {"key1", "value1"},
            {"key2", 123},
            {"key3", true}
        };
        var variable = new Variable<Dictionary<string, object>>("dictVar", null!, "dictVar");
        var setVariable = new SetVariable<Dictionary<string, object>>(variable, new Input<Dictionary<string, object>>(dictionaryValue, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.NotNull(result);
        Assert.Equal(dictionaryValue.Count, result.Count);
        Assert.Equal(dictionaryValue["key1"], result["key1"]);
        Assert.Equal(dictionaryValue["key2"], result["key2"]);
        Assert.Equal(dictionaryValue["key3"], result["key3"]);
    }

    [Fact]
    public async Task Should_Evaluate_Variable_Name_Via_Expression()
    {
        // Arrange - Use an expression for the variable name
        var dynamicVariableName = "dynamic_var_" + DateTime.Now.Ticks;
        var variable = new Variable<string>(dynamicVariableName, "", "dynamicVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Dynamic Value", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Dynamic Value", result);
        Assert.Equal(dynamicVariableName, variable.Name);
    }

    [Fact]
    public async Task Should_Evaluate_Value_Via_Expression()
    {
        // Arrange - Use a computed expression for the value
        var computedValue = $"Computed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        var variable = new Variable<string>("expressionVar", "", "expressionVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>(computedValue, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(computedValue, result);
        Assert.Contains("Computed at", result);
    }

    [Fact]
    public async Task Should_Handle_Expression_That_Returns_Null()
    {
        // Arrange - Expression that evaluates to null
        var variable = new Variable<string>("nullExpressionVar", "initial", "nullExpressionVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>((string)null!, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Handle_Undefined_Expression_Result()
    {
        // Arrange - Test handling of undefined/default values
        var variable = new Variable<int?>("undefinedVar", null, "undefinedVar");
        var setVariable = new SetVariable<int?>(variable, new Input<int?>((int?)null, "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Handle_Zero_Length_Variable_Name()
    {
        // Arrange
        var variable = new Variable<string>(string.Empty, "test", "emptyNameVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("value", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("value", result);
    }

    [Fact]
    public async Task Should_Handle_Unicode_Variable_Names()
    {
        // Arrange - Test with Unicode characters in variable names
        var unicodeVariableName = "变量名_متغیر_переменная_🚀";
        var variable = new Variable<string>(unicodeVariableName, "", "unicodeVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Unicode Value", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Unicode Value", result);
        Assert.Equal(unicodeVariableName, variable.Name);
    }

    [Fact]
    public async Task Should_Handle_Extremely_Long_Variable_Name()
    {
        // Arrange - Test with very long variable name
        var longVariableName = new string('a', 1000);
        var variable = new Variable<string>(longVariableName, "", "longNameVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Long Name Value", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Long Name Value", result);
        Assert.Equal(1000, variable.Name.Length);
    }

    [Fact]
    public async Task Should_Handle_Numeric_Variable_Names()
    {
        // Arrange - Test with numeric variable names
        var numericVariableName = "12345";
        var variable = new Variable<string>(numericVariableName, "", "numericVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Numeric Name Value", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Numeric Name Value", result);
        Assert.Equal(numericVariableName, variable.Name);
    }

    [Fact]
    public async Task Should_Handle_Variable_Name_With_Spaces()
    {
        // Arrange - Test with spaces in variable names
        var spacedVariableName = "variable with spaces";
        var variable = new Variable<string>(spacedVariableName, "", "spacedVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Spaced Name Value", "inputId"));
        
        // Act
        var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Spaced Name Value", result);
        Assert.Equal(spacedVariableName, variable.Name);
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Variable_Updates()
    {
        // Arrange - Test thread safety with concurrent updates
        var variable = new Variable<int>("concurrentVar", 0, "concurrentVar");
        var tasks = new List<Task<int>>();
        
        // Act - Create multiple concurrent assignments
        for (int i = 1; i <= 10; i++)
        {
            var value = i;
            tasks.Add(Task.Run(async () =>
            {
                var setVariable = new SetVariable<int>(variable, new Input<int>(value, $"input{value}"));
                var context = await ActivityTestHelper.ExecuteActivityAsync(setVariable);
                return variable.Get(context);
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert - All operations should complete successfully
        Assert.Equal(10, results.Length);
        Assert.All(results, result => Assert.True(result >= 1 && result <= 10));
    }
}

/// <summary>
/// Complex test object for serialization testing
/// </summary>
public class ComplexTestObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public string[] Items { get; set; } = [];
}
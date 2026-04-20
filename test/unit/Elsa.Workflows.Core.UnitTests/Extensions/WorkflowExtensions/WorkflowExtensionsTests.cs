using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.Extensions.WorkflowExtensions;

public class WorkflowExtensionsTests
{
    private const string VariableTestValuesKey = "VariableTestValues";

    [Fact]
    public void CreatedWithModernTooling_ReturnsFalse_WhenToolVersionIsNull()
    {
        var workflow = new Workflow { WorkflowMetadata = new WorkflowMetadata() };
        Assert.False(workflow.CreatedWithModernTooling());
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(2, 9, 0)]
    public void CreatedWithModernTooling_ReturnsFalse_WhenToolVersionMajorIsLessThan3(int major, int minor, int patch)
    {
        var workflow = new Workflow
        {
            WorkflowMetadata = new WorkflowMetadata(ToolVersion: new Version(major, minor, patch))
        };
        Assert.False(workflow.CreatedWithModernTooling());
    }

    [Theory]
    [InlineData(3, 0, 0)]
    [InlineData(4, 0, 0)]
    public void CreatedWithModernTooling_ReturnsTrue_WhenToolVersionMajorIsAtLeast3(int major, int minor, int patch)
    {
        var workflow = new Workflow
        {
            WorkflowMetadata = new WorkflowMetadata(ToolVersion: new Version(major, minor, patch))
        };
        Assert.True(workflow.CreatedWithModernTooling());
    }

    [Fact]
    public void WhenCreatedWithModernTooling_ExecutesModernAction_WhenToolVersionIs3OrHigher()
    {
        var workflow = new Workflow
        {
            WorkflowMetadata = new WorkflowMetadata(ToolVersion: new Version(3, 0, 0))
        };
        var modernCalled = false;
        var legacyCalled = false;

        workflow.WhenCreatedWithModernTooling(() => modernCalled = true, () => legacyCalled = true);

        Assert.True(modernCalled);
        Assert.False(legacyCalled);
    }

    [Fact]
    public void WhenCreatedWithModernTooling_ExecutesLegacyAction_WhenToolVersionIsBelow3()
    {
        var workflow = new Workflow
        {
            WorkflowMetadata = new WorkflowMetadata(ToolVersion: new Version(2, 0, 0))
        };
        var modernCalled = false;
        var legacyCalled = false;

        workflow.WhenCreatedWithModernTooling(() => modernCalled = true, () => legacyCalled = true);

        Assert.False(modernCalled);
        Assert.True(legacyCalled);
    }

    [Fact]
    public void GetTestVariables_ReturnsEmptyDictionary_WhenNoTestValuesSet()
    {
        var workflow = new Workflow();
        var result = workflow.GetTestVariables();
        Assert.Empty(result);
    }

    [Fact]
    public void GetTestVariables_ReturnsValues_ForMatchingVariables()
    {
        var variable = new Variable<int> { Id = "var1", Name = "MyVar" };
        var workflow = new Workflow();
        workflow.Variables.Add(variable);
        SetTestVariable(workflow, "var1", 42);

        var result = workflow.GetTestVariables();

        Assert.Single(result);
        Assert.Equal(42, result["var1"]);
    }

    [Fact]
    public void GetTestVariables_ExcludesValues_ForNonExistentVariables()
    {
        var variable = new Variable<int> { Id = "var1", Name = "MyVar" };
        var workflow = new Workflow();
        workflow.Variables.Add(variable);
        SetTestVariable(workflow, "var1", 10);
        SetTestVariable(workflow, "nonexistent", 99);

        var result = workflow.GetTestVariables();

        Assert.Single(result);
        Assert.True(result.ContainsKey("var1"));
        Assert.False(result.ContainsKey("nonexistent"));
    }

    [Fact]
    public void GetTestVariables_ConvertsValueToVariableType()
    {
        var variable = new Variable<double> { Id = "var1", Name = "MyVar" };
        var workflow = new Workflow();
        workflow.Variables.Add(variable);
        SetTestVariable(workflow, "var1", 42);

        var result = workflow.GetTestVariables();

        Assert.IsType<double>(result["var1"]);
        Assert.Equal(42.0, result["var1"]);
    }

    [Fact]
    public void GetTestVariables_HandlesJsonElement_WhenDeserializedFromJson()
    {
        var variable = new Variable<string> { Id = "var1", Name = "MyVar" };
        var workflow = new Workflow();
        workflow.Variables.Add(variable);

        var json = JsonSerializer.Serialize(new Dictionary<string, object?> { ["var1"] = "hello" });
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        workflow.CustomProperties[VariableTestValuesKey] = jsonElement;

        var result = workflow.GetTestVariables();

        Assert.Single(result);
        Assert.Equal("hello", result["var1"]);
    }

    [Fact]
    public void GetTestVariables_ReturnsMultipleVariables()
    {
        var var1 = new Variable<int> { Id = "v1", Name = "Var1" };
        var var2 = new Variable<string> { Id = "v2", Name = "Var2" };
        var workflow = new Workflow();
        workflow.Variables.Add(var1);
        workflow.Variables.Add(var2);
        SetTestVariable(workflow, "v1", 10);
        SetTestVariable(workflow, "v2", "text");

        var result = workflow.GetTestVariables();

        Assert.Equal(2, result.Count);
        Assert.Equal(10, result["v1"]);
        Assert.Equal("text", result["v2"]);
    }

    [Fact]
    public void GetTestVariables_HandlesEnumerableKeyValuePairs()
    {
        var variable = new Variable<int> { Id = "var1", Name = "MyVar" };
        var workflow = new Workflow();
        workflow.Variables.Add(variable);

        var dict = new Dictionary<string, object?> { ["var1"] = 5 };
        workflow.CustomProperties[VariableTestValuesKey] = dict;

        var result = workflow.GetTestVariables();

        Assert.Single(result);
        Assert.Equal(5, result["var1"]);
    }

    /// <summary>
    /// Helper to set test variable values via CustomProperties, since SetTestVariable is internal.
    /// </summary>
    private static void SetTestVariable(Workflow workflow, string variableId, object? value)
    {
        if (!workflow.CustomProperties.TryGetValue(VariableTestValuesKey, out var raw) || raw is not Dictionary<string, object?> collection)
        {
            collection = new Dictionary<string, object?>();
            workflow.CustomProperties[VariableTestValuesKey] = collection;
        }
        collection[variableId] = value;
    }
}

using System.Dynamic;
using System.Text.Json;
using Elsa.Expressions.JavaScript.Activities;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Namotion.Reflection;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class ActivityTestRunnerTests
{
    [Fact]
    public async Task RunAsync_WithNoCustomProperties_DoesNotSetInputOrVariables()
    {
        // Arrange
        var activity = new WriteLine("Hello");
        var fixture = new ActivityTestFixture(activity);
        var context = await fixture.BuildAsync();
        var workflow = context.WorkflowExecutionContext.Workflow;

        // Assert: No custom properties set by default.
        Assert.False(workflow.CustomProperties.ContainsKey(IActivityTestRunner.VariableTestValuesPropertyName));
    }


    [Fact]
    public async Task RunAsync_WithTestProperty_ProducedExpectedOutcome()
    {
        // Arrange
        var variableName = "Name";
        var expectedValue = "John";
        var variable = new Variable<string>(variableName, "default");
        var javaScript = $$"""return `${getVariable("{{variableName}}")}`;""";
        var activity = new RunJavaScript(script: javaScript) { Result = new Output<object?>() };

        // Setup execution context with the activity and a test variable value.
        ActivityExecutionContext activityExecutionContext = await new ActivityTestFixture(activity)
            .ConfigureContext(c =>
            {
                var workflow = c.WorkflowExecutionContext.Workflow;
                workflow.Variables.Add(variable);
                workflow.SetTestVariable(variableName, expectedValue);
            })
            .ConfigureServices(services => services
                .AddScoped<ActivityTestRunner>()
                .AddOptions()
                .AddScoped<IConfiguration>(sp => new ConfigurationBuilder().Build())
                .AddElsa(m => m
                    .UseWorkflows()
                    .UseJavaScript()))
            .BuildAsync();

        // Act: Create the subject and run the test runner.
        var subject = activityExecutionContext.GetRequiredService<ActivityTestRunner>();
        var workflowGraph = activityExecutionContext.WorkflowExecutionContext.WorkflowGraph;
        activityExecutionContext = await subject.RunAsync(workflowGraph, activity, CancellationToken.None);

        // Assert: The output of the JavaScript activity should be the expected value, demonstrating that the variable test value was correctly applied and evaluated.
        Assert.Empty(activityExecutionContext.WorkflowExecutionContext.Incidents);
        Assert.Equal(expectedValue, activity.GetOutput(activityExecutionContext, "Result"));
    }



    [Fact]
    public async Task RunAsync_WithVariableTestValues_AsJsonElement_ParsesCorrectly()
    {
        // Arrange
        var variable = new Variable<int>("Counter", 0);
        var activity = new WriteLine("Hello");
        var fixture = new ActivityTestFixture(activity);
        var context = await fixture.BuildAsync();
        var workflow = context.WorkflowExecutionContext.Workflow;

        workflow.Variables.Add(variable);

        // Add VariableTestValues as a JsonElement (simulating round-trip from JSON storage).
        var testValues = new Dictionary<string, object> { [variable.Id] = 42 };
        var json = JsonSerializer.Serialize(testValues);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
        workflow.CustomProperties[IActivityTestRunner.VariableTestValuesPropertyName] = jsonElement;

        // Assert: The test values are stored as a JsonElement.
        Assert.IsType<JsonElement>(workflow.CustomProperties[IActivityTestRunner.VariableTestValuesPropertyName]);
    }

    [Fact]
    public async Task RunAsync_WithVariableTestValues_AsExpandoObject_ParsesCorrectly()
    {
        // Arrange
        var variable = new Variable<string>("Name", "default");
        var activity = new WriteLine("Hello");
        var fixture = new ActivityTestFixture(activity);
        var context = await fixture.BuildAsync();
        var workflow = context.WorkflowExecutionContext.Workflow;

        workflow.Variables.Add(variable);

        // Add VariableTestValues as an ExpandoObject (simulating already-deserialized data).
        var expando = new ExpandoObject() as IDictionary<string, object?>;
        expando[variable.Id] = "OverriddenValue";
        workflow.CustomProperties[IActivityTestRunner.VariableTestValuesPropertyName] = (ExpandoObject)expando;

        // Assert: The test values are stored as an ExpandoObject.
        Assert.IsType<ExpandoObject>(workflow.CustomProperties[IActivityTestRunner.VariableTestValuesPropertyName]);
    }

    [Fact]
    public async Task RunAsync_WithVariableTestValues_SkipsUnknownVariableIds()
    {
        // Arrange
        var variable = new Variable<int>("Counter", 0);
        var activity = new WriteLine("Hello");
        var fixture = new ActivityTestFixture(activity);
        var context = await fixture.BuildAsync();
        var workflow = context.WorkflowExecutionContext.Workflow;

        workflow.Variables.Add(variable);

        // Add test values with a known variable ID and a non-existent one.
        var expando = new ExpandoObject() as IDictionary<string, object?>;
        expando[variable.Id] = 99;
        expando["nonExistentVariableId"] = 123;
        workflow.CustomProperties[IActivityTestRunner.VariableTestValuesPropertyName] = (ExpandoObject)expando;

        // Assert: Only the known variable exists.
        Assert.Single(workflow.Variables);
    }


    [Fact]
    public void PropertyNames_AreCorrectConstants()
    {
        Assert.Equal("VariableTestValues", IActivityTestRunner.VariableTestValuesPropertyName);
    }

    [Fact]
    public async Task RunAsync_WithMultipleVariables_ParsesEachCorrectly()
    {
        // Arrange
        var intVar = new Variable<int>("Counter", 0);
        var stringVar = new Variable<string>("Name", "default");
        var activity = new WriteLine("Hello");
        var fixture = new ActivityTestFixture(activity);
        var context = await fixture.BuildAsync();
        var workflow = context.WorkflowExecutionContext.Workflow;

        workflow.Variables.Add(intVar);
        workflow.Variables.Add(stringVar);

        // Add test values for both variables.
        var expando = new ExpandoObject() as IDictionary<string, object?>;
        expando[intVar.Id] = 100;
        expando[stringVar.Id] = "Hello World";
        workflow.CustomProperties[IActivityTestRunner.VariableTestValuesPropertyName] = (ExpandoObject)expando;

        // Assert: Both variables are present.
        Assert.Equal(2, workflow.Variables.Count);
        Assert.Contains(workflow.Variables, v => v.Id == intVar.Id);
        Assert.Contains(workflow.Variables, v => v.Id == stringVar.Id);
    }
}
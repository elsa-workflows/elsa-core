using Elsa.Bpmn.Exceptions;
using Elsa.Workflows;
using Xunit.Abstractions;
using static Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime.BpmnRuntimeTestHost;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Runtime;

/// <summary>
/// Sequence flows conditional on standard BPMN <c>conditionExpression</c>s, imported from XML and run end to end. Elsa
/// evaluates each expression with its own expression engine, against the process's data (ADR "Evaluate BPMN conditions
/// with Elsa expressions").
/// </summary>
public class BpmnConditionTests(ITestOutputHelper testOutputHelper) : IAsyncLifetime
{
    private readonly BpmnRuntimeTestHost _host = new(testOutputHelper);

    public Task InitializeAsync() => _host.InitializeAsync();

    public Task DisposeAsync() => _host.DisposeAsync();

    [Theory(DisplayName = "An exclusive gateway takes the first flow whose condition holds, else its default flow")]
    [InlineData(5000, "Large")]
    [InlineData(500, "Medium")]
    [InlineData(5, "Small")]
    public async Task ExclusiveGateway_RoutesOnTheConditionsElsaEvaluates(int amount, string expectedTask)
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("expression-condition-routing.bpmn");

        // Act
        var instanceId = await _host.StartAsync(definitionId, input: new Dictionary<string, object> { ["amount"] = amount });
        var finished = await _host.WaitForAsync(instanceId, IsFinished);

        // Assert: exactly the expected task ran. 5000 satisfies both conditions; the gateway takes the first declared.
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);

        var statuses = await _host.ActivityStatusesOfAsync(instanceId);
        string[] tasks = ["Large", "Medium", "Small"];

        foreach (var task in tasks)
        {
            Assert.Equal(task == expectedTask, statuses.ContainsKey(WorkActivityId("route-by-amount", task)));
        }
    }

    [Theory(DisplayName = "A condition takes its flow when it evaluates to true, and leaves it to the default when false")]
    [InlineData(true, "Yes")]
    [InlineData(false, "No")]
    public async Task BooleanCondition_Routes(bool value, string expectedTask)
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("expression-condition-input-value.bpmn");

        // Act
        var instanceId = await _host.StartAsync(definitionId, input: new Dictionary<string, object> { ["value"] = value });
        var finished = await _host.WaitForAsync(instanceId, IsFinished);

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        var statuses = await _host.ActivityStatusesOfAsync(instanceId);
        Assert.True(statuses.ContainsKey(WorkActivityId("condition-input-value", expectedTask)));
    }

    [Theory(DisplayName = "A condition that evaluates to something other than a boolean faults the process instead of choosing a flow")]
    [InlineData(1)]
    [InlineData("oops")]
    [InlineData("true")]
    public async Task NonBooleanCondition_FaultsTheProcess(object value)
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("expression-condition-input-value.bpmn");

        // Act
        var instanceId = await _host.StartAsync(definitionId, input: new Dictionary<string, object> { ["value"] = value });
        var finished = await _host.WaitForAsync(instanceId, IsFinished);

        // Assert: neither flow was taken.
        Assert.Equal(WorkflowSubStatus.Faulted, finished.SubStatus);
        var incident = Assert.Single(finished.Incidents);
        Assert.StartsWith("bpmn.condition.failed", incident.Message);
        Assert.Contains("not a boolean", incident.Message);

        var statuses = await _host.ActivityStatusesOfAsync(instanceId);
        Assert.False(statuses.ContainsKey(WorkActivityId("condition-input-value", "Yes")));
        Assert.False(statuses.ContainsKey(WorkActivityId("condition-input-value", "No")));
    }

    [Fact(DisplayName = "A Liquid condition that renders the text true takes its flow")]
    public async Task LiquidCondition_Routes()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("expression-condition-liquid.bpmn");

        // Act
        var instanceId = await _host.StartAsync(definitionId);
        var finished = await _host.WaitForAsync(instanceId, IsFinished);

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Empty(finished.Incidents);
        Assert.True((await _host.ActivityStatusesOfAsync(instanceId)).ContainsKey(WorkActivityId("condition-liquid", "Yes")));
    }

    [Fact(DisplayName = "A condition that cannot be evaluated faults the process with the expression's error")]
    public async Task FailingCondition_FaultsTheProcess()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("expression-condition-failing.bpmn");

        // Act
        var instanceId = await _host.StartAsync(definitionId);
        var finished = await _host.WaitForAsync(instanceId, IsFinished);

        // Assert
        Assert.Equal(WorkflowSubStatus.Faulted, finished.SubStatus);
        var incident = Assert.Single(finished.Incidents);
        Assert.Equal(typeof(BpmnScopeFaultException), incident.Exception?.Type);
        Assert.StartsWith("bpmn.condition.failed", incident.Message);
        Assert.Contains("Flow_Broken", incident.Message);
        Assert.Contains("noSuchFunction", incident.Message);
    }

    [Fact(DisplayName = "An exclusive gateway whose conditions all evaluate false and that has no default flow faults the process")]
    public async Task ExclusiveGatewayWithNoMatch_FaultsTheProcess()
    {
        // Arrange
        var definitionId = await _host.ImportAndPublishAsync("expression-condition-no-match.bpmn");

        // Act
        var instanceId = await _host.StartAsync(definitionId);
        var finished = await _host.WaitForAsync(instanceId, IsFinished);

        // Assert
        Assert.Equal(WorkflowSubStatus.Faulted, finished.SubStatus);
        var incident = Assert.Single(finished.Incidents);
        Assert.StartsWith("bpmn.gateway.no-flow-selected", incident.Message);
    }
}

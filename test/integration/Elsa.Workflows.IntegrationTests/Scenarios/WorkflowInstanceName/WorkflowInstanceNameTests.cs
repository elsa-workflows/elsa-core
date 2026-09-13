using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.WorkflowInstanceName.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowInstanceName;

public class WorkflowInstanceNameTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowRuntime _workflowRuntime;

    public WorkflowInstanceNameTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddWorkflow<NamedWorkflow>()
            .Build();

        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
    }

    [Test]
    [DisplayName("Setting a workflow instance name keeps the workflow instance name when the workflow is executed")]
    public async Task SuspendedCancelTest()
    {
        await _services.PopulateRegistriesAsync();
        const string workflowDefinitionId = nameof(NamedWorkflow);
        var desiredName = Guid.NewGuid().ToString();
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            Name = desiredName,
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        var workflowState = await workflowClient.ExportStateAsync();

        await Assert.That(_capturingTextWriter.Lines).IsEquivalentTo([desiredName], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(workflowState.Name).IsEqualTo(desiredName);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}

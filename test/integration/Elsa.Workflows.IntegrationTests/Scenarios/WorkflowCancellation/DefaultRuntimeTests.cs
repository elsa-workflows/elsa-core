using Elsa.Common.Models;
using Elsa.Mediator.HostedServices;
using Elsa.Mediator.Options;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation;

/// <summary>
/// Represents a class containing unit tests for the DefaultRuntime class.
/// </summary>
public class DefaultRuntimeTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRuntimeTests"/> class.
    /// </summary>
    public DefaultRuntimeTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddWorkflow<BulkSuspendedWorkflow>()
            .AddWorkflow<ResumeDispatchWorkflow>()
            .AddWorkflow<SimpleChildWorkflow>()
            .AddWorkflow<SimpleSuspendedWorkflow>()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton(sp =>
                    {
                        var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                        return ActivatorUtilities.CreateInstance<BackgroundCommandSenderHostedService>(sp, options.CommandWorkerCount);
                    })
                    .AddSingleton(sp =>
                    {
                        var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                        return ActivatorUtilities.CreateInstance<BackgroundEventPublisherHostedService>(sp, options.NotificationWorkerCount);
                    })
                    ;
            })
            .Build();

        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
    }

    [Fact(DisplayName = "Cancelling a suspended workflow")]
    public async Task SuspendedCancelTest()
    {
        await _services.PopulateRegistriesAsync();

        const string workflowDefinitionId = nameof(SimpleSuspendedWorkflow);
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published)
        });
        var runWorkflowInstanceResponse = await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        Assert.Equal(WorkflowStatus.Running, runWorkflowInstanceResponse.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, runWorkflowInstanceResponse.SubStatus);

        await workflowClient.CancelAsync();
        var lastWorkflowState = await workflowClient.ExportStateAsync();

        Assert.Equal(WorkflowStatus.Finished, lastWorkflowState!.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, lastWorkflowState.SubStatus);
        Assert.Empty(_capturingTextWriter.Lines);
    }
}
using Elsa.Mediator.HostedServices;
using Elsa.Mediator.Options;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowCancellation;

public class DefaultRuntimeTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly BackgroundCommandSenderHostedService _backgroundCommandSenderHostedService;
    private readonly BackgroundEventPublisherHostedService _backgroundEventPublisherHostedService;

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

        _backgroundCommandSenderHostedService = _services.GetRequiredService<BackgroundCommandSenderHostedService>();
        _backgroundEventPublisherHostedService = _services.GetRequiredService<BackgroundEventPublisherHostedService>();
        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
    }

    [Fact(DisplayName = "Cancelling a suspended workflow")]
    public async Task SuspendedCancelTest()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        const string workflowDefinitionId = nameof(SimpleSuspendedWorkflow);
        var workflowState = await _workflowRuntime.StartWorkflowAsync(workflowDefinitionId, new StartWorkflowRuntimeOptions());

        Assert.Equal(WorkflowStatus.Running, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

        await _workflowRuntime.CancelWorkflowAsync(workflowState.WorkflowInstanceId);
        var lastWorkflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);

        Assert.Equal(WorkflowStatus.Finished, lastWorkflowState!.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, lastWorkflowState.SubStatus);
        Assert.Empty(_capturingTextWriter.Lines);
    }

    [Fact(DisplayName = "Cancelling a running workflow",
        Skip = "Unpredictable result, need to create a dispatcher for tests that will run outside of the unit test")]
    public async Task RunningCancelTest()
    {
        await _backgroundCommandSenderHostedService.StartAsync(CancellationToken.None);
        await _backgroundEventPublisherHostedService.StartAsync(CancellationToken.None);

        // Populate registries.
        await _services.PopulateRegistriesAsync();

        const string workflowDefinitionId = nameof(BulkSuspendedWorkflow);
        var workflowState = await _workflowRuntime.StartWorkflowAsync(workflowDefinitionId, new StartWorkflowRuntimeOptions());

        var bookmarks = new Stack<Bookmark>(workflowState.Bookmarks);
        var resumeOptions = new ResumeWorkflowRuntimeOptions
        {
            BookmarkId = bookmarks.Pop().Id
        };
        var state = await _workflowRuntime.ResumeWorkflowAsync(workflowState.WorkflowInstanceId, resumeOptions);

        await _workflowRuntime.CancelWorkflowAsync(workflowState.WorkflowInstanceId);
        var lastWorkflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);

        Assert.Equal(WorkflowStatus.Finished, lastWorkflowState!.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, lastWorkflowState.SubStatus);
        Assert.NotEmpty(_capturingTextWriter.Lines);
    }
}
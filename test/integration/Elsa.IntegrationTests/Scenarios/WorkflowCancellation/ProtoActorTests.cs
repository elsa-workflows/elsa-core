using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.IntegrationTests.Scenarios.WorkflowCancellation.Workflows;
using Elsa.Mediator.HostedServices;
using Elsa.Mediator.Options;
using Elsa.ProtoActor.HostedServices;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Proto.Persistence.Sqlite;
using Xunit;
using Xunit.Abstractions;
using WorkflowStatus = Elsa.Workflows.WorkflowStatus;
using WorkflowSubStatus = Elsa.Workflows.WorkflowSubStatus;

namespace Elsa.IntegrationTests.Scenarios.WorkflowCancellation;

public class ProtoActorTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly WorkflowServerHost _workflowServerHost;
    private readonly BackgroundCommandSenderHostedService _backgroundCommandSenderHostedService;
    private readonly BackgroundEventPublisherHostedService _backgroundEventPublisherHostedService;

    public ProtoActorTests(ITestOutputHelper testOutputHelper)
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
                    .AddSingleton(sp => ActivatorUtilities.CreateInstance<WorkflowServerHost>(sp));

                services
                    .AddSingleton(sp =>
                    {
                        var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                        return ActivatorUtilities.CreateInstance<BackgroundCommandSenderHostedService>(sp,
                            options.CommandWorkerCount);
                    })
                    .AddSingleton(sp =>
                    {
                        var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                        return ActivatorUtilities.CreateInstance<BackgroundEventPublisherHostedService>(sp,
                            options.NotificationWorkerCount);
                    });
            }).ConfigureElsa(elsa => elsa.UseWorkflowRuntime(runtime => runtime.UseProtoActor(protoActor =>
                {
                    protoActor.PersistenceProvider = _ =>
                        new SqliteProvider(
                            new SqliteConnectionStringBuilder("Data Source=elsa.sqlite.db;Cache=Shared;"));
                }
            )))
            .Build();

        _backgroundCommandSenderHostedService = _services.GetRequiredService<BackgroundCommandSenderHostedService>();
        _backgroundEventPublisherHostedService = _services.GetRequiredService<BackgroundEventPublisherHostedService>();
        _workflowServerHost = _services.GetRequiredService<WorkflowServerHost>();
        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
    }

    [Fact(DisplayName = "Cancelling a suspended workflow")]
    public async Task SuspendedCancelTest()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();
        await _workflowServerHost.StartAsync(default);
        const string workflowDefinitionId = nameof(SimpleSuspendedWorkflow);
        var workflowState =
            await _workflowRuntime.StartWorkflowAsync(workflowDefinitionId, new StartWorkflowRuntimeOptions());

        Assert.Equal(WorkflowStatus.Running, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);

        await _workflowRuntime.CancelWorkflowAsync(workflowState.WorkflowInstanceId);

        await Task.Delay(2000);
        var lastWorkflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);

        Assert.Equal(WorkflowStatus.Finished, lastWorkflowState!.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, lastWorkflowState.SubStatus);
        Assert.Empty(_capturingTextWriter.Lines);
    }

    [Fact(DisplayName = "Cancelling a running workflow")]
    public async Task RunningCancelTest()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();
        await _workflowServerHost.StartAsync(default);
        await _backgroundCommandSenderHostedService.StartAsync(CancellationToken.None);
        await _backgroundEventPublisherHostedService.StartAsync(CancellationToken.None);

        const string workflowDefinitionId = nameof(BulkSuspendedWorkflow);
        var workflowState =
            await _workflowRuntime.StartWorkflowAsync(workflowDefinitionId, new StartWorkflowRuntimeOptions());

        var bookmarks = new Stack<Bookmark>(workflowState.Bookmarks);
        var resumeOptions = new ResumeWorkflowRuntimeOptions { BookmarkId = bookmarks.Pop().Id };
        await _workflowRuntime.ResumeWorkflowAsync(workflowState.WorkflowInstanceId, resumeOptions);

        await _workflowRuntime.CancelWorkflowAsync(workflowState.WorkflowInstanceId);
        var lastWorkflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowState.WorkflowInstanceId);

        Assert.Equal(WorkflowStatus.Finished, lastWorkflowState!.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, lastWorkflowState.SubStatus);

        Assert.NotEmpty(_capturingTextWriter.Lines);
    }
}
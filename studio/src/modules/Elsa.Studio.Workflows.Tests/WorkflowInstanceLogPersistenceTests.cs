using System.Reflection;
using System.Text.Json;
using Bunit;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class WorkflowInstanceLogPersistenceTests
{
    [Theory]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Strategy","strategyType":"Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core"}}}""", "Exclude", false)]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Strategy","strategyType":"Elsa.Workflows.LogPersistence.Strategies.Inherit, Elsa.Workflows.Core"},"internalState":{"evaluationMode":"Strategy","strategyType":"Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core"}}}""", "Inherit", false)]
    [InlineData("""{"logPersistenceMode":{"default":"Include"}}""", "Include", false)]
    [InlineData("""{"logPersistenceMode":{"default":2}}""", "Exclude", false)]
    [InlineData("""{"logPersistenceMode":{"default":0}}""", "Inherit", false)]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Strategy","strategyType":"Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core"}},"logPersistenceMode":{"default":"Exclude"}}""", "Include", false)]
    [InlineData("""{"logPersistenceConfig":{"default":null},"logPersistenceMode":{"default":"Exclude"}}""", "Exclude", false)]
    [InlineData("""{"logPersistenceConfig":{"inputs":{"message":{"evaluationMode":"Strategy","strategyType":"Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core"}}},"logPersistenceMode":{"default":"Include"}}""", "Include", false)]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Expression","expression":{"type":"JavaScript","value":"LogPersistenceMode.Exclude"}}}}""", "LogPersistenceMode.Exclude", true)]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Expression","expression":{"type":"JavaScript","value":"Include"}}}}""", "Include", true)]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Expression","strategyType":"Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core","expression":{"type":"JavaScript","value":"  "}}}}""", "Inherit", false)]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Strategy","strategyType":","}}}""", "Inherit", false)]
    [InlineData("""{"logPersistenceConfig":{"default":{"evaluationMode":"Strategy","strategyType":", Elsa.Workflows.Core"}}}""", "Inherit", false)]
    [InlineData("""{"logPersistenceConfig":{"default":"not-an-object"},"logPersistenceMode":{"default":"Exclude"}}""", "Inherit", false)]
    [InlineData("{}", "Inherit", false)]
    public void Format_ReadsTheWorkflowModeWithoutThrowing(string json, string expected, bool isExpression)
    {
        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        Assert.Equal(new LogPersistenceDisplay(isExpression, expected), WorkflowLogPersistenceDisplay.Format(properties));
    }

    [Fact]
    public void Format_UnsetDefinitionInherits()
    {
        Assert.Equal(new LogPersistenceDisplay(false, "Inherit"), WorkflowLogPersistenceDisplay.Format(null));
    }
}

public sealed class WorkflowInstanceDetailsLogPersistenceTests : BunitContext, IAsyncLifetime
{
    public WorkflowInstanceDetailsLogPersistenceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IClipboard>(new ClipboardStub());
        Services.AddSingleton<ITimeFormatter>(new TimeFormatterStub());
        Services.AddSingleton<IStorageDriverService>(new StorageDriverStub());
        Services.AddSingleton<IWorkflowInstanceService>(new WorkflowInstanceStub());
        Services.AddSingleton(DispatchProxy.Create<IActivityRegistry, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IActivityExecutionService, ThrowingProxy>());
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void DetailsPanelShowsTheLegacyLogPersistenceMode()
    {
        var definition = new WorkflowDefinition
        {
            Id = "version-1",
            DefinitionId = "definition-1",
            Name = "Order",
            CustomProperties = JsonSerializer.Deserialize<Dictionary<string, object>>("""{"logPersistenceMode":{"default":"Include"}}""")!
        };
        var instance = new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = definition.DefinitionId,
            DefinitionVersionId = definition.Id,
            Version = 64,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished,
            WorkflowState = new WorkflowState(),
            CreatedAt = DateTimeOffset.Parse("2024-04-18T08:17:36Z"),
            UpdatedAt = DateTimeOffset.Parse("2024-04-18T08:17:36Z"),
            FinishedAt = DateTimeOffset.Parse("2024-04-18T08:17:36Z")
        };

        var cut = Render<WorkflowInstanceDetails>(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.WorkflowInstance, instance));

        var row = cut.FindAll("tr").Single(tr => tr.TextContent.Contains("Log Persistence Strategy"));
        Assert.Contains("Include", row.TextContent);
    }

    private sealed class ClipboardStub : IClipboard
    {
        public Task CopyText(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TimeFormatterStub : ITimeFormatter
    {
        public string Format(DateTimeOffset? value, string format = "G", string emptyString = "") => value?.ToString("u") ?? emptyString;
    }

    private sealed class StorageDriverStub : IStorageDriverService
    {
        public Task<IEnumerable<StorageDriverDescriptor>> GetStorageDriversAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<StorageDriverDescriptor>>([]);
    }

    private sealed class WorkflowInstanceStub : IWorkflowInstanceService
    {
        public Task<IEnumerable<ResolvedVariable>> GetVariablesAsync(string instanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<ResolvedVariable>>([]);

        public Task<PagedListResponse<WorkflowInstanceSummary>> ListAsync(ListWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkDeleteAsync(IEnumerable<string> instanceIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CancelAsync(string instanceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task BulkCancelAsync(BulkCancelWorkflowInstancesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowInstance?> GetAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedListResponse<WorkflowExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> BulkExportAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BulkImportAsync(IEnumerable<StreamPart> streamParts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private class ThrowingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected call to {targetMethod!.DeclaringType!.Name}.{targetMethod.Name}.");
    }
}

using Bunit;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class ActivityExecutionDetailsTests : BunitContext, IAsyncLifetime
{
    private readonly ActivityExecutionServiceStub _executionService = new();

    public ActivityExecutionDetailsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<ITimeFormatter, TestTimeFormatter>();
        Services.AddSingleton<IActivityExecutionService>(_executionService);
        Services.AddSingleton<IClipboard, ClipboardStub>();
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void RendersStateOutcomesAndOutputForTheSelectedExecution()
    {
        var record = new ActivityExecutionRecord
        {
            Id = "exec-1",
            ActivityId = "WriteLine1",
            ActivityNodeId = "Workflow1:WriteLine1",
            ActivityType = "Elsa.WriteLine",
            Status = ActivityStatus.Completed,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            ActivityState = new Dictionary<string, object?>
            {
                ["Text"] = "hello",
                ["_internal"] = "hidden"
            },
            Payload = new Dictionary<string, object?>
            {
                ["Outcomes"] = "Done"
            },
            Outputs = new Dictionary<string, object?>
            {
                ["Result"] = "ok"
            }
        };

        var cut = Render<ActivityExecutionDetails>(parameters => parameters
            .Add(details => details.ActivityExecution, record));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("State", cut.Markup);
            Assert.Contains("Text", cut.Markup);
            Assert.Contains("hello", cut.Markup);
            Assert.DoesNotContain("_internal", cut.Markup);
            Assert.Contains("Outcomes", cut.Markup);
            Assert.Contains("Done", cut.Markup);
            Assert.Contains("Output", cut.Markup);
            Assert.Contains("Result", cut.Markup);
            Assert.Contains("ok", cut.Markup);
        });
    }

    [Fact]
    public void RendersRetryAttemptsWhenTheExecutionHasRetries()
    {
        _executionService.Retries = new PagedListResponse<RetryAttemptRecord>
        {
            Items =
            [
                new RetryAttemptRecord
                {
                    AttemptNumber = 1,
                    RetryDelay = TimeSpan.FromSeconds(1),
                    Details = new Dictionary<string, string?> { ["Reason"] = "timeout" }
                }
            ],
            TotalCount = 1
        };

        var record = new ActivityExecutionRecord
        {
            Id = "exec-retry",
            ActivityId = "HttpRequest1",
            ActivityNodeId = "Workflow1:HttpRequest1",
            ActivityType = "Elsa.HttpRequest",
            Status = ActivityStatus.Completed,
            StartedAt = DateTimeOffset.UtcNow
        };

        var cut = Render<ActivityExecutionDetails>(parameters => parameters
            .Add(details => details.ActivityExecution, record));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Retry Attempts", cut.Markup);
            Assert.Contains("timeout", cut.Markup);
        });
    }

    private sealed class ActivityExecutionServiceStub : IActivityExecutionService
    {
        public PagedListResponse<RetryAttemptRecord> Retries { get; set; } = new();

        public Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, System.Text.Json.Nodes.JsonObject containerActivity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ActivityExecutionRecord>());

        public Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ActivityExecutionRecordSummary>());

        public Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ActivityExecutionRecord?>(null);

        public Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedListResponse<RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Retries);
    }

    private sealed class ClipboardStub : IClipboard
    {
        public Task CopyText(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestTimeFormatter : ITimeFormatter
    {
        public string Format(DateTimeOffset? value, string format = "G", string emptyString = "") =>
            value?.ToString(format) ?? emptyString;
    }
}

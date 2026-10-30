using Bunit;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class ExecutionDetailsDrawerRenderTests : BunitContext, IAsyncLifetime
{
    private readonly ActivityExecutionServiceStub _executionService = new();

    public ExecutionDetailsDrawerRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<ITimeFormatter, TestTimeFormatter>();
        Services.AddSingleton<IClipboard, ClipboardStub>();
        Services.AddSingleton<IActivityExecutionService>(_executionService);
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void SelectingAnExecution_MountsTheOpenDrawerWithThatExecutionsDetails()
    {
        var record = CreateRecord("exec-1", "hello", "Done", "ok");
        _executionService.Record = record;

        var cut = Render<ExecutionSelectionHost>(parameters => parameters
            .Add(host => host.Summaries, [CreateSummary("exec-1"), CreateSummary("exec-2")]));

        Assert.Empty(cut.FindAll("aside.execution-details-drawer"));

        cut.FindAll("tbody tr")[0].Click();

        cut.WaitForAssertion(() =>
        {
            var drawer = cut.Find("aside.execution-details-drawer");
            Assert.Contains("mud-drawer--open", drawer.ClassList);
            Assert.Contains("mud-drawer-temporary", drawer.ClassList);
            Assert.Contains("Execution Details", cut.Markup);
            Assert.Contains("hello", cut.Markup);
            Assert.Contains("Done", cut.Markup);
            Assert.Contains("ok", cut.Markup);
        });
    }

    [Fact]
    public void HeaderClose_DismissesTheDrawer()
    {
        var cut = Render<DrawerHost>(parameters => parameters
            .Add(host => host.InitiallyOpen, true)
            .Add(host => host.ActivityExecution, CreateRecord("exec-1", "hello", "Done", "ok")));

        Assert.NotEmpty(cut.FindAll("aside.execution-details-drawer"));

        cut.Find("button[aria-label='Close']").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("aside.execution-details-drawer")));
    }

    [Fact]
    public async Task OverlayDismissal_UnmountsTheDrawer()
    {
        var cut = Render<DrawerHost>(parameters => parameters
            .Add(host => host.InitiallyOpen, true)
            .Add(host => host.ActivityExecution, CreateRecord("exec-1", "hello", "Done", "ok")));

        var drawer = cut.FindComponent<MudDrawer>();
        await cut.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(false));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("aside.execution-details-drawer")));
    }

    private static ActivityExecutionRecord CreateRecord(string id, string text, string outcome, string output) => new()
    {
        Id = id,
        ActivityId = "WriteLine1",
        ActivityNodeId = "Workflow1:WriteLine1",
        ActivityType = "Elsa.WriteLine",
        Status = ActivityStatus.Completed,
        StartedAt = DateTimeOffset.UtcNow,
        ActivityState = new Dictionary<string, object?> { ["Text"] = text },
        Payload = new Dictionary<string, object?> { ["Outcomes"] = outcome },
        Outputs = new Dictionary<string, object?> { ["Result"] = output }
    };

    private static ActivityExecutionRecordSummary CreateSummary(string id) => new()
    {
        Id = id,
        ActivityId = "WriteLine1",
        ActivityNodeId = "Workflow1:WriteLine1",
        ActivityType = "Elsa.WriteLine",
        Status = ActivityStatus.Completed,
        StartedAt = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// Owns drawer open-state the same way <c>WorkflowInstanceViewer</c> does.
    /// </summary>
    private sealed class DrawerHost : ComponentBase
    {
        [Parameter] public bool InitiallyOpen { get; set; }
        [Parameter] public ActivityExecutionRecord? ActivityExecution { get; set; }

        private bool _open;

        protected override void OnInitialized() => _open = InitiallyOpen;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<ActivityExecutionDetailsDrawer>(0);
            builder.AddAttribute(1, "Open", _open);
            builder.AddAttribute(2, "ActivityExecution", ActivityExecution);
            builder.AddAttribute(3, "OpenChanged", EventCallback.Factory.Create<bool>(this, OnOpenChanged));
            builder.CloseComponent();
        }

        private void OnOpenChanged(bool open) => _open = open;
    }

    /// <summary>
    /// Same wiring as <c>WorkflowInstanceViewer</c>: Executions-tab row click loads the record and opens the production drawer.
    /// The full viewer also mounts Journal, the designer, and SignalR, which is not practical in bUnit.
    /// </summary>
    private sealed class ExecutionSelectionHost : ComponentBase
    {
        [Parameter] public ICollection<ActivityExecutionRecordSummary> Summaries { get; set; } = [];
        [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;

        private bool _open;
        private ActivityExecutionRecord? _record;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<ActivityExecutionsTab>(0);
            builder.AddAttribute(1, "ActivityExecutionSummaries", Summaries);
            builder.AddAttribute(2, "VisiblePaneHeight", 400);
            builder.AddAttribute(3, "ExecutionSelected", EventCallback.Factory.Create<string>(this, OnExecutionSelected));
            builder.CloseComponent();

            builder.OpenComponent<ActivityExecutionDetailsDrawer>(4);
            builder.AddAttribute(5, "Open", _open);
            builder.AddAttribute(6, "ActivityExecution", _record);
            builder.AddAttribute(7, "OpenChanged", EventCallback.Factory.Create<bool>(this, OnOpenChanged));
            builder.CloseComponent();
        }

        private async Task OnExecutionSelected(string executionId)
        {
            _record = await ActivityExecutionService.GetAsync(executionId);
            _open = _record != null;
        }

        private void OnOpenChanged(bool open) => _open = open;
    }

    private sealed class ActivityExecutionServiceStub : IActivityExecutionService
    {
        public ActivityExecutionRecord? Record { get; set; }

        public Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, System.Text.Json.Nodes.JsonObject containerActivity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ActivityExecutionRecord>());

        public Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ActivityExecutionRecordSummary>());

        public Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == Record?.Id ? Record : null);

        public Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedListResponse<Elsa.Api.Client.Resources.Resilience.Models.RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedListResponse<Elsa.Api.Client.Resources.Resilience.Models.RetryAttemptRecord>());
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

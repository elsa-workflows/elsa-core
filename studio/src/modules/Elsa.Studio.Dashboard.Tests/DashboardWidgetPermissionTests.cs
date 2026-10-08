using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

/// <summary>
/// Dashboard widgets only link to, and load data from, what the user can view.
/// </summary>
public sealed class DashboardWidgetPermissionTests : BunitContext, IAsyncLifetime
{
    private readonly StubOpenTelemetryService _telemetry = new();
    private readonly DashboardWidgetContext _context;

    public DashboardWidgetPermissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IOpenTelemetryService>(_telemetry);
        _context = new("24h", false, DateTimeOffset.UtcNow, DashboardLoadStatus.Loaded, null,
            new DashboardSnapshot(new(), new(), new(), new(), null), () => Task.CompletedTask, Services.GetRequiredService<NavigationManager>());
    }

    [Theory]
    [InlineData(typeof(ConsoleLogsDashboardWidget), "diagnostics/console-logs:view", "Open console")]
    [InlineData(typeof(StructuredLogsDashboardWidget), "diagnostics/structured-logs:view", "Open logs")]
    public void DiagnosticsWidgets_LinkToTheirPageOnlyWithViewPermission(Type widget, string permission, string link)
    {
        Assert.DoesNotContain(link, RenderWidget(widget).Markup);
        Assert.Contains(link, RenderWidget(widget, permission).Markup);
    }

    [Fact]
    public void OpenTelemetryWidget_IsNeitherShownNorLoadedWithoutViewPermission()
    {
        var cut = RenderWidget(typeof(OpenTelemetryDashboardWidget));

        Assert.Empty(cut.Markup.Trim());
        Assert.Equal(0, _telemetry.StorageDiagnosticsCalls);
    }

    [Fact]
    public void OpenTelemetryWidget_LoadsWithViewPermission()
    {
        RenderWidget(typeof(OpenTelemetryDashboardWidget), "diagnostics/opentelemetry:view");

        Assert.Equal(1, _telemetry.StorageDiagnosticsCalls);
    }

    [Fact]
    public void WorkflowMetrics_ShowWithoutLinkingToInstancesTheUserCannotView()
    {
        Assert.Empty(RenderWidget(typeof(DashboardWorkflowMetricsWidget)).FindAll("a.dashboard-operational-health-link"));
        Assert.NotEmpty(RenderWidget(typeof(DashboardWorkflowMetricsWidget), "workflows/instances:view").FindAll("a.dashboard-operational-health-link"));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    private IRenderedComponent<CascadingValue<UserPermissions>> RenderWidget(Type widget, params string[] grants) =>
        Render<CascadingValue<UserPermissions>>(parameters => parameters
            .Add(x => x.Value, StubPermissionService.Grants(grants))
            .Add(x => x.ChildContent, builder =>
            {
                builder.OpenComponent(0, widget);
                builder.AddComponentParameter(1, "Context", _context);
                builder.CloseComponent();
            }));

    private sealed class StubOpenTelemetryService : IOpenTelemetryService
    {
        public int StorageDiagnosticsCalls { get; private set; }

        public Task<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            StorageDiagnosticsCalls++;
            return Task.FromResult(new OpenTelemetryStorageDiagnostics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }

        public Task<OpenTelemetryResourceResult> GetResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryTraceResult> GetTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryMetricResult> GetMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OpenTelemetryLogResult> GetLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CollectorConfiguration?> GetCollectorConfigurationAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

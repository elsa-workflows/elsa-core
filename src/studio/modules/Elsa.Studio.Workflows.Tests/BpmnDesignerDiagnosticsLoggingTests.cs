using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDesigner.LoadBpmnAsync"/>'s handling of the view model's diagnostics: every
/// diagnostic the JS adapter reports must be logged, at a severity that reflects the diagnostic's own
/// severity, rather than only "error" diagnostics being logged and the rest silently dropped.
/// </summary>
public sealed class BpmnDesignerDiagnosticsLoggingTests : BunitContext, IAsyncLifetime
{
    private readonly CapturingLogger<BpmnDesigner> _logger = new();

    public BpmnDesignerDiagnosticsLoggingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddWorkflowsCore();
        Services.AddWorkflowsDesigner();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry, NoOpActivityRegistry>();
        Services.AddSingleton<ILogger<BpmnDesigner>>(_logger);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task LoadBpmnAsync_LogsEveryDiagnostic_AtASeverityThatReflectsItsOwn()
    {
        JSInterop.Setup<BpmnDiagnostic[]>("loadBpmnDiagram", _ => true).SetResult(
        [
            new BpmnDiagnostic("BPMN001", "error", "Missing start event", "Process_1", null),
            new BpmnDiagnostic("BPMN002", "warning", "Unbound lane", "Lane_1", null),
            new BpmnDiagnostic("BPMN003", "info", "Documentation ignored", "Doc_1", null)
        ]);

        var activity = CreateActivity();
        var cut = Render<BpmnDesigner>(parameters => parameters.Add(p => p.Activity, activity));

        await cut.InvokeAsync(() => cut.Instance.LoadBpmnAsync(activity, null, null));

        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("BPMN001") && e.Message.Contains("Process_1"));
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("BPMN002") && e.Message.Contains("Lane_1"));
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("BPMN003") && e.Message.Contains("Doc_1"));
    }

    private static JsonObject CreateActivity() => new()
    {
        ["id"] = "root",
        ["type"] = "Elsa.BpmnProcess",
        ["activities"] = new JsonArray()
    };

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class NoOpActivityRegistry : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => [];
        public ActivityDescriptor? Find(string activityType, int? version = default) => null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => [];
        public void MarkStale()
        {
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}

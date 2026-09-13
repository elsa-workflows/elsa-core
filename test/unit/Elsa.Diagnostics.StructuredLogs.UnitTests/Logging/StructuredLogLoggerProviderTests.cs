using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Logging;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.Logging;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.Logging;

public class StructuredLogLoggerProviderTests
{
    private readonly CapturingStructuredLogProvider _logProvider = new();
    private readonly StructuredLogSourceRegistry _sourceRegistry;
    private readonly StructuredLogLoggerProvider _loggerProvider;

    public StructuredLogLoggerProviderTests()
    {
        var options = MicrosoftOptions.Create(new StructuredLogsOptions());
        _sourceRegistry = new(options);
        _loggerProvider = new(_logProvider, new StructuredLogRedactor(options), _sourceRegistry, options);
    }

    [After(Test)]
    public void DisposeLoggerProvider() => _loggerProvider.Dispose();

    [Test]
    public async Task Log_WhenStructuredWarningIsWritten_PublishesRedactedStructuredLogEvent()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");
        var exception = new InvalidOperationException("password=letmein");

        logger.LogWarning(new EventId(42, "WorkflowFaulted"), exception, "Token {AccessToken}", "secret-token");

        var logEvent = await Assert.That(_logProvider.Events).HasSingleItem();
        await Assert.That(logEvent.Level).IsEqualTo(StructuredLogLevel.Warning);
        await Assert.That(logEvent.Category).IsEqualTo("Elsa.Workflows.Runtime");
        await Assert.That(logEvent.EventId).IsEqualTo(42);
        await Assert.That(logEvent.EventName).IsEqualTo("WorkflowFaulted");
        await Assert.That(logEvent.MessageTemplate).IsEqualTo("Token {AccessToken}");
        await Assert.That(logEvent.Properties["AccessToken"]).IsEqualTo("[Redacted]");
        await Assert.That(logEvent.Exception!.Message).IsEqualTo("[Redacted]");
        await Assert.That(logEvent.SourceId).IsEqualTo(_sourceRegistry.Current.Id);
    }

    [Test]
    public async Task Log_WhenTemplateHasNamedArguments_CapturesTemplateAndStructuredProperties()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");

        logger.LogInformation("Workflow {WorkflowInstanceId} started for {TenantId}", "workflow-instance-a", "tenant-a");

        var logEvent = await Assert.That(_logProvider.Events).HasSingleItem();
        await Assert.That(logEvent.MessageTemplate).IsEqualTo("Workflow {WorkflowInstanceId} started for {TenantId}");
        await Assert.That(logEvent.Properties["WorkflowInstanceId"]).IsEqualTo("workflow-instance-a");
        await Assert.That(logEvent.Properties["TenantId"]).IsEqualTo("tenant-a");
        await Assert.That(logEvent.Properties.ContainsKey("{OriginalFormat}")).IsFalse();
        await Assert.That(logEvent.WorkflowInstanceId).IsEqualTo("workflow-instance-a");
        await Assert.That(logEvent.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    public async Task Log_WhenScopeIsActive_CapturesAndRedactsScopeValues()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["TenantId"] = "tenant-a",
            ["Password"] = "secret"
        });
        logger.LogInformation("Scoped event");

        var logEvent = await Assert.That(_logProvider.Events).HasSingleItem();
        await Assert.That(logEvent.Scopes["TenantId"]).IsEqualTo("tenant-a");
        await Assert.That(logEvent.Scopes["Password"]).IsEqualTo("[Redacted]");
        await Assert.That(logEvent.TenantId).IsEqualTo("tenant-a");
    }

    [Test]
    public async Task Log_WhenStringScopeIsActive_CapturesRenderedScope()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");

        using var scope = logger.BeginScope("outer-scope");
        logger.LogInformation("Scoped event");

        var logEvent = await Assert.That(_logProvider.Events).HasSingleItem();
        await Assert.That(logEvent.Scopes["Scope"]).IsEqualTo("outer-scope");
    }

    [Test]
    public async Task Log_WhenCategoryIsStructuredLogsInternal_DoesNotPublishByDefault()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Diagnostics.StructuredLogs.Services.StructuredLogSourceRegistry");

        logger.LogInformation("Internal structured logs chatter");

        await Assert.That(_logProvider.Events).IsEmpty();
    }

    private sealed class CapturingStructuredLogProvider : IStructuredLogProvider
    {
        public List<StructuredLogEvent> Events { get; } = new();

        public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(logEvent);
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}

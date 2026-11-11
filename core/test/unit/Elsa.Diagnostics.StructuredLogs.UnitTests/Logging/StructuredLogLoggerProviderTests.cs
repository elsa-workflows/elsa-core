using Elsa.Common.Multitenancy;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Logging;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.Logging;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

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

    [Fact]
    public void Log_WhenStructuredWarningIsWritten_PublishesRedactedStructuredLogEvent()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");
        var exception = new InvalidOperationException("password=letmein");

        logger.LogWarning(new EventId(42, "WorkflowFaulted"), exception, "Token {AccessToken}", "secret-token");

        var logEvent = Assert.Single(_logProvider.Events);
        Assert.Equal(StructuredLogLevel.Warning, logEvent.Level);
        Assert.Equal("Elsa.Workflows.Runtime", logEvent.Category);
        Assert.Equal(42, logEvent.EventId);
        Assert.Equal("WorkflowFaulted", logEvent.EventName);
        Assert.Equal("Token {AccessToken}", logEvent.MessageTemplate);
        Assert.Equal("[Redacted]", logEvent.Properties["AccessToken"]);
        Assert.Equal("[Redacted]", logEvent.Exception!.Message);
        Assert.Equal(_sourceRegistry.Current.Id, logEvent.SourceId);
    }

    [Fact]
    public void Log_WhenTemplateHasNamedArguments_CapturesTemplateAndStructuredProperties()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");

        logger.LogInformation("Workflow {WorkflowInstanceId} started for {TenantId}", "workflow-instance-a", "tenant-a");

        var logEvent = Assert.Single(_logProvider.Events);
        Assert.Equal("Workflow {WorkflowInstanceId} started for {TenantId}", logEvent.MessageTemplate);
        Assert.Equal("workflow-instance-a", logEvent.Properties["WorkflowInstanceId"]);
        Assert.Equal("tenant-a", logEvent.Properties["TenantId"]);
        Assert.False(logEvent.Properties.ContainsKey("{OriginalFormat}"));
        Assert.Equal("workflow-instance-a", logEvent.WorkflowInstanceId);
        Assert.Equal("tenant-a", logEvent.TenantId);
    }

    [Fact]
    public void Log_WhenScopeIsActive_CapturesAndRedactsScopeValues()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["TenantId"] = "tenant-a",
            ["Password"] = "secret"
        });
        logger.LogInformation("Scoped event");

        var logEvent = Assert.Single(_logProvider.Events);
        Assert.Equal("tenant-a", logEvent.Scopes["TenantId"]);
        Assert.Equal("[Redacted]", logEvent.Scopes["Password"]);
        Assert.Equal("tenant-a", logEvent.TenantId);
    }

    [Fact]
    public void Log_WhenStringScopeIsActive_CapturesRenderedScope()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");

        using var scope = logger.BeginScope("outer-scope");
        logger.LogInformation("Scoped event");

        var logEvent = Assert.Single(_logProvider.Events);
        Assert.Equal("outer-scope", logEvent.Scopes["Scope"]);
    }

    [Fact]
    public void Log_WhenCategoryIsStructuredLogsInternal_DoesNotPublishByDefault()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Diagnostics.StructuredLogs.Services.StructuredLogSourceRegistry");

        logger.LogInformation("Internal structured logs chatter");

        Assert.Empty(_logProvider.Events);
    }

    [Fact]
    public void Log_WhenTenantContextIsActive_StampsAmbientTenant()
    {
        var logger = CreateLogger(new TestTenantAccessor("tenant-a"));

        logger.LogInformation("Event without tenant property");

        Assert.Equal("tenant-a", Assert.Single(_logProvider.Events).TenantId);
    }

    [Fact]
    public void Log_WhenEventNamesItsOwnTenant_KeepsNamedTenant()
    {
        var logger = CreateLogger(new TestTenantAccessor("tenant-a"));

        logger.LogInformation("Tenant {TenantId} activated", "tenant-b");

        Assert.Equal("tenant-b", Assert.Single(_logProvider.Events).TenantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Log_WhenThereIsNoTenantContext_LeavesEventUntagged(string? tenantId)
    {
        var logger = CreateLogger(tenantId == null ? null : new TestTenantAccessor(tenantId));

        logger.LogInformation("Host event");

        Assert.Null(Assert.Single(_logProvider.Events).TenantId);
    }

    private ILogger CreateLogger(ITenantAccessor? tenantAccessor)
    {
        var options = MicrosoftOptions.Create(new StructuredLogsOptions());
        return new StructuredLogLoggerProvider(_logProvider, new StructuredLogRedactor(options), _sourceRegistry, options, tenantAccessor).CreateLogger("Elsa.Workflows.Runtime");
    }

    private sealed class TestTenantAccessor(string tenantId) : ITenantAccessor
    {
        public string TenantId { get; } = tenantId;
        public Tenant? Tenant => null;
        public IDisposable PushContext(Tenant? tenant) => throw new NotSupportedException();
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

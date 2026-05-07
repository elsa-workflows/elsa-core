using Elsa.Diagnostics.Contracts;
using Elsa.Diagnostics.Logging;
using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Options;
using Elsa.Diagnostics.Services;
using Microsoft.Extensions.Logging;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.UnitTests.Logging;

public class ServerLogLoggerProviderTests
{
    private readonly CapturingServerLogProvider _logProvider = new();
    private readonly ServerLogSourceRegistry _sourceRegistry;
    private readonly ServerLogLoggerProvider _loggerProvider;

    public ServerLogLoggerProviderTests()
    {
        var options = MicrosoftOptions.Create(new ServerLogStreamingOptions());
        _sourceRegistry = new(options);
        _loggerProvider = new(_logProvider, new ServerLogRedactor(options), _sourceRegistry, options);
    }

    [Fact]
    public void Log_WhenStructuredWarningIsWritten_PublishesRedactedServerLogEvent()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Workflows.Runtime");
        var exception = new InvalidOperationException("password=letmein");

        logger.LogWarning(new EventId(42, "WorkflowFaulted"), exception, "Token {AccessToken}", "secret-token");

        var logEvent = Assert.Single(_logProvider.Events);
        Assert.Equal(ServerLogLevel.Warning, logEvent.Level);
        Assert.Equal("Elsa.Workflows.Runtime", logEvent.Category);
        Assert.Equal(42, logEvent.EventId);
        Assert.Equal("WorkflowFaulted", logEvent.EventName);
        Assert.Equal("[Redacted]", logEvent.Properties["AccessToken"]);
        Assert.Equal("[Redacted]", logEvent.Exception!.Message);
        Assert.Equal(_sourceRegistry.Current.Id, logEvent.SourceId);
    }

    [Fact]
    public void Log_WhenCategoryIsDiagnosticsInternal_DoesNotPublishByDefault()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.Diagnostics.Services.ServerLogSourceRegistry");

        logger.LogInformation("Internal diagnostics chatter");

        Assert.Empty(_logProvider.Events);
    }

    private sealed class CapturingServerLogProvider : IServerLogProvider
    {
        public List<ServerLogEvent> Events { get; } = new();

        public ValueTask PublishAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(logEvent);
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<ServerLogEvent> SubscribeAsync(ServerLogFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyCollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}

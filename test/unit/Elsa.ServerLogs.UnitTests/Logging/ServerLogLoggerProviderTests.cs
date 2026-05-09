using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Logging;
using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Options;
using Elsa.ServerLogs.Services;
using Microsoft.Extensions.Logging;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.ServerLogs.UnitTests.Logging;

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
    public void Log_WhenCategoryIsServerLogsInternal_DoesNotPublishByDefault()
    {
        var logger = _loggerProvider.CreateLogger("Elsa.ServerLogs.Services.ServerLogSourceRegistry");

        logger.LogInformation("Internal server logs chatter");

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

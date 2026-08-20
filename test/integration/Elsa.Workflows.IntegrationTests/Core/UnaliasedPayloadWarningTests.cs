using System.Collections.Concurrent;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Core;

/// <summary>
/// A payload whose type has no registered serialization alias is stored without a type discriminator and read back
/// as a property bag with camel-cased keys. That is deliberate — the alias registry is an allow-list that keeps
/// arbitrary CLR type names out of deserialization — but it is lossy, so it is worth saying out loud once.
/// </summary>
public class UnaliasedPayloadWarningTests(ITestOutputHelper testOutputHelper)
{
    // A distinct type per test: the warning is deduplicated per type for the lifetime of the process, so two tests
    // sharing a payload type would not be independent.
    private class UnaliasedPayload { public string? Status { get; set; } }
    private class RepeatedlySerializedPayload { public string? Status { get; set; } }
    private class AliasedPayload { public string? Status { get; set; } }
    private class PayloadSerializedWhileWarningsOff { public string? Status { get; set; } }

    [Fact]
    public void UnaliasedPayload_WarnsNamingTheTypeAndTheRemedy()
    {
        var (serializer, log) = Build();

        serializer.Serialize(StateWith(new UnaliasedPayload { Status = "Shipped" }));

        var warning = Assert.Single(log.Warnings);
        Assert.Contains(nameof(UnaliasedPayload), warning);
        Assert.Contains("AddTypeAlias", warning);
    }

    [Fact]
    public void UnaliasedPayload_WarnsOnlyOncePerType()
    {
        var (serializer, log) = Build();
        var payload = new RepeatedlySerializedPayload { Status = "Shipped" };

        serializer.Serialize(StateWith(payload));
        serializer.Serialize(StateWith(payload));
        serializer.Serialize(StateWith(new RepeatedlySerializedPayload { Status = "Delivered" }));

        Assert.Single(log.Warnings, x => x.Contains(nameof(RepeatedlySerializedPayload)));
    }

    [Fact]
    public void AliasedPayload_DoesNotWarnAndKeepsItsPropertyCasing()
    {
        var (serializer, log) = Build(options => options.AddTypeAlias<AliasedPayload>());

        var json = serializer.Serialize(StateWith(new AliasedPayload { Status = "Shipped" }));
        var readBack = serializer.Deserialize(json).Output["Payload"];

        Assert.DoesNotContain(log.Warnings, x => x.Contains(nameof(AliasedPayload)));
        Assert.Equal("Shipped", Assert.IsType<AliasedPayload>(readBack).Status);
    }

    [Fact]
    public void Dictionary_DoesNotWarnAndKeepsItsKeysVerbatim()
    {
        var (serializer, log) = Build();

        var json = serializer.Serialize(StateWith(new Dictionary<string, object> { ["Status"] = "Shipped" }));
        var readBack = serializer.Deserialize(json).Output["Payload"];

        Assert.Empty(log.Warnings);
        Assert.Equal("Shipped", Assert.IsAssignableFrom<IDictionary<string, object>>(readBack)["Status"]);
    }

    [Fact]
    public void WarningSuppressedByLogLevel_IsStillReportedOnceTheLevelIsRaised()
    {
        var (serializer, log) = Build();
        log.Enabled = false;
        var payload = new PayloadSerializedWhileWarningsOff { Status = "Shipped" };

        serializer.Serialize(StateWith(payload));
        log.Enabled = true;
        serializer.Serialize(StateWith(payload));

        Assert.Single(log.Warnings, x => x.Contains(nameof(PayloadSerializedWhileWarningsOff)));
    }

    private static WorkflowState StateWith(object payload) => new()
    {
        Id = "instance-1",
        DefinitionId = "definition-1",
        DefinitionVersionId = "version-1",
        Output = { ["Payload"] = payload }
    };

    private (IWorkflowStateSerializer Serializer, LogCapture Log) Build(Action<SerializationTypeOptions>? configureAliases = null)
    {
        var log = new LogCapture();
        var builder = new TestApplicationBuilder(testOutputHelper)
            // The capture must be the only provider: IsEnabled on the composite logger is an OR across providers, so
            // leaving the builder's own xunit provider in place would keep Warning enabled whatever the capture says.
            .ConfigureServices(services => services.AddLogging(logging => logging.ClearProviders().AddProvider(log)));

        if (configureAliases != null)
            builder.ConfigureServices(services => services.Configure(configureAliases));

        return (builder.Build().GetRequiredService<IWorkflowStateSerializer>(), log);
    }

    private class LogCapture : ILoggerProvider, ILogger
    {
        private readonly ConcurrentQueue<string> _warnings = new();
        public IEnumerable<string> Warnings => _warnings;
        public bool Enabled { get; set; } = true;
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => Enabled && logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (Enabled && logLevel >= LogLevel.Warning)
                _warnings.Enqueue(formatter(state, exception));
        }

        public void Dispose()
        {
        }
    }
}

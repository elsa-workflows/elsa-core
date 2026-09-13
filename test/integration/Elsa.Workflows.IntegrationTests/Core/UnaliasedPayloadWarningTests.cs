using System.Collections.Concurrent;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.IntegrationTests.Core;

/// <summary>
/// A payload whose type has no registered serialization alias is stored without a type discriminator and read back
/// as a property bag with camel-cased keys. That is deliberate — the alias registry is an allow-list that keeps
/// arbitrary CLR type names out of deserialization — but it is lossy, so it is worth saying out loud once.
/// </summary>
public class UnaliasedPayloadWarningTests
{
    // A distinct type per test: the warning is deduplicated per type for the lifetime of the process, so two tests
    // sharing a payload type would not be independent.
    private class UnaliasedPayload { public string? Status { get; set; } }
    private class RepeatedlySerializedPayload { public string? Status { get; set; } }
    private class AliasedPayload { public string? Status { get; set; } }
    private class PayloadSerializedWhileWarningsOff { public string? Status { get; set; } }

    [Test]
    public async Task UnaliasedPayload_WarnsNamingTheTypeAndTheRemedy()
    {
        await using var services = Build(out var log);
        var serializer = services.GetRequiredService<IWorkflowStateSerializer>();

        serializer.Serialize(StateWith(new UnaliasedPayload { Status = "Shipped" }));

        var warning = await Assert.That(log.Warnings).HasSingleItem();
        await Assert.That(warning).Contains(nameof(UnaliasedPayload), StringComparison.CurrentCulture);
        await Assert.That(warning).Contains("AddTypeAlias", StringComparison.CurrentCulture);
    }

    [Test]
    public async Task UnaliasedPayload_WarnsOnlyOncePerType()
    {
        await using var services = Build(out var log);
        var serializer = services.GetRequiredService<IWorkflowStateSerializer>();
        var payload = new RepeatedlySerializedPayload { Status = "Shipped" };

        serializer.Serialize(StateWith(payload));
        serializer.Serialize(StateWith(payload));
        serializer.Serialize(StateWith(new RepeatedlySerializedPayload { Status = "Delivered" }));

        await Assert.That(log.Warnings.Where(x => x.Contains(nameof(RepeatedlySerializedPayload)))).HasSingleItem();
    }

    [Test]
    public async Task AliasedPayload_DoesNotWarnAndKeepsItsPropertyCasing()
    {
        await using var services = Build(out var log, options => options.AddTypeAlias<AliasedPayload>());
        var serializer = services.GetRequiredService<IWorkflowStateSerializer>();

        var json = serializer.Serialize(StateWith(new AliasedPayload { Status = "Shipped" }));
        var readBack = serializer.Deserialize(json).Output["Payload"];

        await Assert.That(log.Warnings).DoesNotContain(x => x.Contains(nameof(AliasedPayload)));
        await Assert.That(readBack).IsOfType(typeof(AliasedPayload));
        var payload = (AliasedPayload)readBack!;
        await Assert.That(payload.Status).IsEqualTo("Shipped");
    }

    [Test]
    public async Task Dictionary_DoesNotWarnAndKeepsItsKeysVerbatim()
    {
        await using var services = Build(out var log);
        var serializer = services.GetRequiredService<IWorkflowStateSerializer>();

        var json = serializer.Serialize(StateWith(new Dictionary<string, object> { ["Status"] = "Shipped" }));
        var readBack = serializer.Deserialize(json).Output["Payload"];

        await Assert.That(log.Warnings).IsEmpty();
        await Assert.That(readBack).IsAssignableTo<IDictionary<string, object>>();
        var dictionary = (IDictionary<string, object>)readBack!;
        await Assert.That(dictionary["Status"]).IsEqualTo("Shipped");
    }

    [Test]
    public async Task WarningSuppressedByLogLevel_IsStillReportedOnceTheLevelIsRaised()
    {
        await using var services = Build(out var log);
        var serializer = services.GetRequiredService<IWorkflowStateSerializer>();
        log.Enabled = false;
        var payload = new PayloadSerializedWhileWarningsOff { Status = "Shipped" };

        serializer.Serialize(StateWith(payload));
        log.Enabled = true;
        serializer.Serialize(StateWith(payload));

        await Assert.That(log.Warnings.Where(x => x.Contains(nameof(PayloadSerializedWhileWarningsOff)))).HasSingleItem();
    }

    private static WorkflowState StateWith(object payload) => new()
    {
        Id = "instance-1",
        DefinitionId = "definition-1",
        DefinitionVersionId = "version-1",
        Output = { ["Payload"] = payload }
    };

    private static ServiceProvider Build(out LogCapture log, Action<SerializationTypeOptions>? configureAliases = null)
    {
        var logCapture = new LogCapture();
        log = logCapture;
        var builder = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            // The capture must be the only provider: IsEnabled on the composite logger is an OR across providers, so
            // leaving the builder's own test-output provider in place would keep Warning enabled whatever the capture says.
            .ConfigureServices(services => services.AddLogging(logging => logging.ClearProviders().AddProvider(logCapture)));

        if (configureAliases != null)
            builder.ConfigureServices(services => services.Configure(configureAliases));

        return (ServiceProvider)builder.Build();
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

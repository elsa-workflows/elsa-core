using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Extensions;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.AI.Host.UnitTests;

public class AiRegistrationTests
{
    [Fact(DisplayName = "AddAiHostServices registers default host services and options")]
    public void AddAiHostServicesRegistersDefaults()
    {
        var services = new ServiceCollection();

        services.AddAiHostServices(options => options.ReconnectGrace = TimeSpan.FromSeconds(30));

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IAiConversationStore>());
        Assert.NotNull(provider.GetRequiredService<AiToolEnablementService>());
        Assert.NotNull(provider.GetRequiredService<IAiAuditSink>());
        Assert.Equal(TimeSpan.FromSeconds(30), provider.GetRequiredService<IOptions<AiHostOptions>>().Value.ReconnectGrace);
    }

    [Fact(DisplayName = "Ai audit sink resolves scoped handlers per record call")]
    public async Task AiAuditSinkResolvesScopedHandlersPerRecordCall()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddScoped<ScopedAuditHandler>();
        services.AddScoped<IAiAuditEventHandler>(sp => sp.GetRequiredService<ScopedAuditHandler>());
        ScopedAuditHandler.RecordedCount = 0;

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAiAuditSink>();

        await sink.RecordAsync(new AiAuditEvent { Type = "chat.started", ActorId = "user-1" });

        Assert.Equal(1, ScopedAuditHandler.RecordedCount);
    }

    [Fact(DisplayName = "Ai audit sink isolates handler failures")]
    public async Task AiAuditSinkIsolatesHandlerFailures()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddScoped<IAiAuditEventHandler, ThrowingAuditHandler>();
        services.AddScoped<ScopedAuditHandler>();
        services.AddScoped<IAiAuditEventHandler>(sp => sp.GetRequiredService<ScopedAuditHandler>());
        ScopedAuditHandler.RecordedCount = 0;

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAiAuditSink>();

        await sink.RecordAsync(new AiAuditEvent { Type = "chat.started", ActorId = "user-1" });

        Assert.Equal(1, ScopedAuditHandler.RecordedCount);
    }

    [Fact(DisplayName = "Ai audit sink propagates cancellation")]
    public async Task AiAuditSinkPropagatesCancellation()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddScoped<IAiAuditEventHandler, CancellingAuditHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAiAuditSink>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await sink.RecordAsync(new AiAuditEvent { Type = "chat.started", ActorId = "user-1" }));
    }


    [Fact(DisplayName = "Tool enablement supports concurrent access")]
    public void ToolEnablementSupportsConcurrentAccess()
    {
        var service = new AiToolEnablementService();
        var definition = new AiToolDefinition
        {
            Name = "workflow.propose",
            Mutability = AiToolMutability.Proposal
        };

        Parallel.For(0, 1000, index =>
        {
            if (index % 2 == 0)
                service.Enable(definition.Name);
            else
                service.Disable(definition.Name);

            _ = service.IsEnabled(definition);
        });

        service.Enable(definition.Name);
        Assert.True(service.IsEnabled(definition));
    }

    [Fact(DisplayName = "AI host allows context provider overrides on startup")]
    public async Task AiHostAllowsContextProviderOverridesOnStartup()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiContextProvider>(new DuplicateContextProvider("WorkflowDefinition"));

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetServices<IHostedService>().OfType<AiContextProviderValidationHostedService>().Single();

        await validator.StartAsync(CancellationToken.None);
    }

    [Fact(DisplayName = "In-memory conversation store evicts expired conversations")]
    public async Task InMemoryConversationStoreEvictsExpiredConversations()
    {
        var store = new InMemoryAiConversationStore(MicrosoftOptions.Create(new AiHostOptions
        {
            ConversationRetention = TimeSpan.FromMinutes(5)
        }));

        await store.SaveAsync(new AiConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        });

        var result = await store.FindAsync("conversation-1");

        Assert.Null(result);
    }

    private class ScopedAuditHandler : IAiAuditEventHandler
    {
        public static int RecordedCount { get; set; }

        public ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            RecordedCount++;
            return ValueTask.CompletedTask;
        }
    }

    private class ThrowingAuditHandler : IAiAuditEventHandler
    {
        public ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Audit sink unavailable.");
    }

    private class CancellingAuditHandler : IAiAuditEventHandler
    {
        public ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
            throw new OperationCanceledException();
    }

    private class DuplicateContextProvider(string kind) : IAiContextProvider
    {
        public string Kind { get; } = kind;

        public ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiResolvedContext { Kind = Kind });
    }
}

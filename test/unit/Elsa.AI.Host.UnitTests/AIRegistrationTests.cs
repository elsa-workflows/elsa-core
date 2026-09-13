using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Options;
using Elsa.Extensions;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.AI.Host.UnitTests;

public class AIRegistrationTests
{
    [Test]
    [DisplayName("AddAIHostServices registers default host services and options")]
    public async Task AddAIHostServicesRegistersDefaults()
    {
        var services = new ServiceCollection();

        services.AddAIHostServices(options => options.ReconnectGrace = TimeSpan.FromSeconds(30));

        using var provider = services.BuildServiceProvider();
        await Assert.That(provider.GetRequiredService<IAIConversationStore>()).IsNotNull();
        await Assert.That(provider.GetRequiredService<AIToolEnablementService>()).IsNotNull();
        await Assert.That(provider.GetRequiredService<IAIAuditSink>()).IsNotNull();
        await Assert.That(provider.GetRequiredService<IOptions<AIHostOptions>>().Value.ReconnectGrace).IsEqualTo(TimeSpan.FromSeconds(30));
    }

    [Test]
    [DisplayName("AI audit sink resolves scoped handlers per record call")]
    public async Task AIAuditSinkResolvesScopedHandlersPerRecordCall()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        var counter = new AuditCounter();
        services.AddSingleton(counter);
        services.AddScoped<ScopedAuditHandler>();
        services.AddScoped<IAIAuditEventHandler>(sp => sp.GetRequiredService<ScopedAuditHandler>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAIAuditSink>();

        await sink.RecordAsync(new AIAuditEvent { Type = "chat.started", ActorId = "user-1" });

        await Assert.That(counter.RecordedCount).IsEqualTo(1);
    }

    [Test]
    [DisplayName("AI audit sink isolates handler failures")]
    public async Task AIAuditSinkIsolatesHandlerFailures()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        var counter = new AuditCounter();
        services.AddSingleton(counter);
        services.AddScoped<IAIAuditEventHandler, ThrowingAuditHandler>();
        services.AddScoped<ScopedAuditHandler>();
        services.AddScoped<IAIAuditEventHandler>(sp => sp.GetRequiredService<ScopedAuditHandler>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAIAuditSink>();

        await sink.RecordAsync(new AIAuditEvent { Type = "chat.started", ActorId = "user-1" });

        await Assert.That(counter.RecordedCount).IsEqualTo(1);
    }

    [Test]
    [DisplayName("AI audit sink propagates cancellation")]
    public async Task AIAuditSinkPropagatesCancellation()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddScoped<IAIAuditEventHandler, CancellingAuditHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAIAuditSink>();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await sink.RecordAsync(new AIAuditEvent { Type = "chat.started", ActorId = "user-1" }));
    }


    [Test]
    [DisplayName("Tool enablement supports concurrent access")]
    public async Task ToolEnablementSupportsConcurrentAccess()
    {
        var service = new AIToolEnablementService();
        var definition = new AIToolDefinition
        {
            Name = "workflow.propose",
            Mutability = AIToolMutability.Proposal
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
        await Assert.That(service.IsEnabled(definition)).IsTrue();
    }

    [Test]
    [DisplayName("Tool enablement requires explicit proposal tool enablement")]
    public async Task ToolEnablementRequiresExplicitProposalToolEnablement()
    {
        var service = new AIToolEnablementService();
        var definition = new AIToolDefinition
        {
            Name = "workflow.propose",
            Mutability = AIToolMutability.Proposal,
            EnabledByDefault = true
        };

        await Assert.That(service.IsEnabled(definition)).IsFalse();

        service.Enable(definition.Name);

        await Assert.That(service.IsEnabled(definition)).IsTrue();
    }

    [Test]
    [DisplayName("Tool enablement enables read-only tools by default")]
    public async Task ToolEnablementEnablesReadOnlyToolsByDefault()
    {
        var service = new AIToolEnablementService();
        var definition = new AIToolDefinition
        {
            Name = "workflow.inspect",
            Mutability = AIToolMutability.ReadOnly
        };

        await Assert.That(service.IsEnabled(definition)).IsTrue();
    }

    [Test]
    [DisplayName("AI host allows context provider overrides on startup")]
    public async Task AIHostAllowsContextProviderOverridesOnStartup()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIContextProvider>(new DuplicateContextProvider("WorkflowDefinition"));

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetServices<IHostedService>().OfType<AIContextProviderValidationHostedService>().Single();

        await validator.StartAsync(CancellationToken.None);
    }

    [Test]
    [DisplayName("AI host validates scoped context providers from a startup scope")]
    public async Task AIHostValidatesScopedContextProvidersFromStartupScope()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddScoped<ScopedContextProviderDependency>();
        services.AddScoped<IAIContextProvider, ScopedContextProvider>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var validator = provider.GetServices<IHostedService>().OfType<AIContextProviderValidationHostedService>().Single();

        await validator.StartAsync(CancellationToken.None);
    }

    [Test]
    [DisplayName("In-memory conversation store evicts expired conversations")]
    public async Task InMemoryConversationStoreEvictsExpiredConversations()
    {
        var store = new InMemoryAIConversationStore();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            RetentionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        });

        var result = await store.FindAsync("conversation-1");

        await Assert.That(result).IsNull();
    }

    [Test]
    [DisplayName("In-memory conversation store retains ephemeral conversations in process")]
    public async Task InMemoryConversationStoreRetainsEphemeralConversationsInProcess()
    {
        var store = new InMemoryAIConversationStore();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            RetentionMode = AIRetentionMode.Ephemeral,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var result = await store.FindAsync("conversation-1");

        await Assert.That(result).IsNotNull();
    }

    [Test]
    [DisplayName("In-memory conversation store prunes completed ephemeral conversations")]
    public async Task InMemoryConversationStorePrunesCompletedEphemeralConversations()
    {
        var store = new InMemoryAIConversationStore();

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            UserId = "user-1",
            Status = AIConversationStatus.Completed,
            RetentionMode = AIRetentionMode.Ephemeral,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var result = await store.FindAsync("conversation-1");

        await Assert.That(result).IsNull();
    }

    [Test]
    [DisplayName("In-memory conversation store rejects cross-tenant overwrites")]
    public async Task InMemoryConversationStoreRejectsCrossTenantOverwrites()
    {
        var store = new InMemoryAIConversationStore();
        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-2",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }));

        await Assert.That(exception!.Message).IsEqualTo("Cannot overwrite an AI conversation that belongs to another tenant.");
    }

    [Test]
    [DisplayName("In-memory conversation store treats null and empty tenant IDs as default tenant")]
    public async Task InMemoryConversationStoreTreatsNullAndEmptyTenantIdsAsDefaultTenant()
    {
        var store = new InMemoryAIConversationStore();
        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = null,
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var conversation = await store.FindAsync("conversation-1");

        await Assert.That(conversation).IsNotNull();
        await Assert.That(conversation.TenantId).IsEqualTo("");
    }

    [Test]
    [DisplayName("In-memory conversation store treats conversation IDs case-insensitively")]
    public async Task InMemoryConversationStoreTreatsConversationIdsCaseInsensitively()
    {
        var store = new InMemoryAIConversationStore();
        await store.SaveAsync(new AIConversation
        {
            Id = "Conversation-1",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var conversation = await store.FindAsync("conversation-1");

        await Assert.That(conversation).IsNotNull();
        await Assert.That(conversation.Id).IsEqualTo("Conversation-1");
    }

    [Test]
    [DisplayName("In-memory conversation store rejects cross-user overwrites")]
    public async Task InMemoryConversationStoreRejectsCrossUserOverwrites()
    {
        var store = new InMemoryAIConversationStore();
        await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await store.SaveAsync(new AIConversation
        {
            Id = "conversation-1",
            TenantId = "tenant-1",
            UserId = "user-2",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }));

        await Assert.That(exception!.Message).IsEqualTo("Cannot overwrite an AI conversation that belongs to another user.");
    }

    [Test]
    [DisplayName("In-memory conversation store validates required conversation fields")]
    public async Task InMemoryConversationStoreValidatesRequiredConversationFields()
    {
        var store = new InMemoryAIConversationStore();
        var conversation = new AIConversation
        {
            Id = "conversation-invalid"
        };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await store.SaveAsync(conversation));

        await Assert.That(exception!.ParamName).IsEqualTo("conversation");
        await Assert.That(exception.Message).IsEqualTo("A conversation user ID is required. (Parameter 'conversation')");
    }

    private sealed class AuditCounter
    {
        public int RecordedCount { get; set; }
    }

    private class ScopedAuditHandler(AuditCounter counter) : IAIAuditEventHandler
    {

        public ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            counter.RecordedCount++;
            return ValueTask.CompletedTask;
        }
    }

    private class ThrowingAuditHandler : IAIAuditEventHandler
    {
        public ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Audit sink unavailable.");
    }

    private class CancellingAuditHandler : IAIAuditEventHandler
    {
        public ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
            throw new OperationCanceledException();
    }

    private class DuplicateContextProvider(string kind) : IAIContextProvider
    {
        public string Kind { get; } = kind;

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIResolvedContext { Kind = Kind });
    }

    private class ScopedContextProviderDependency
    {
    }

    private class ScopedContextProvider(ScopedContextProviderDependency dependency) : IAIContextProvider
    {
        public string Kind => "WorkflowDefinition";

        public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
        {
            _ = dependency;
            return ValueTask.FromResult(new AIResolvedContext { Kind = Kind });
        }
    }
}

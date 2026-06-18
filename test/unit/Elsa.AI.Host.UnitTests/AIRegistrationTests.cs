using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Options;
using Elsa.Extensions;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.AI.Host.UnitTests;

public class AIRegistrationTests
{
    [Fact(DisplayName = "AddAIHostServices registers default host services and options")]
    public void AddAIHostServicesRegistersDefaults()
    {
        var services = new ServiceCollection();

        services.AddAIHostServices(options => options.ReconnectGrace = TimeSpan.FromSeconds(30));

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IAIConversationStore>());
        Assert.NotNull(provider.GetRequiredService<AIToolEnablementService>());
        Assert.NotNull(provider.GetRequiredService<IAIAuditSink>());
        Assert.Equal(TimeSpan.FromSeconds(30), provider.GetRequiredService<IOptions<AIHostOptions>>().Value.ReconnectGrace);
    }

    [Fact(DisplayName = "AI audit sink resolves scoped handlers per record call")]
    public async Task AIAuditSinkResolvesScopedHandlersPerRecordCall()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddScoped<ScopedAuditHandler>();
        services.AddScoped<IAIAuditEventHandler>(sp => sp.GetRequiredService<ScopedAuditHandler>());
        ScopedAuditHandler.RecordedCount = 0;

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAIAuditSink>();

        await sink.RecordAsync(new AIAuditEvent { Type = "chat.started", ActorId = "user-1" });

        Assert.Equal(1, ScopedAuditHandler.RecordedCount);
    }

    [Fact(DisplayName = "AI audit sink isolates handler failures")]
    public async Task AIAuditSinkIsolatesHandlerFailures()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddScoped<IAIAuditEventHandler, ThrowingAuditHandler>();
        services.AddScoped<ScopedAuditHandler>();
        services.AddScoped<IAIAuditEventHandler>(sp => sp.GetRequiredService<ScopedAuditHandler>());
        ScopedAuditHandler.RecordedCount = 0;

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAIAuditSink>();

        await sink.RecordAsync(new AIAuditEvent { Type = "chat.started", ActorId = "user-1" });

        Assert.Equal(1, ScopedAuditHandler.RecordedCount);
    }

    [Fact(DisplayName = "AI audit sink propagates cancellation")]
    public async Task AIAuditSinkPropagatesCancellation()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddScoped<IAIAuditEventHandler, CancellingAuditHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var sink = provider.GetRequiredService<IAIAuditSink>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await sink.RecordAsync(new AIAuditEvent { Type = "chat.started", ActorId = "user-1" }));
    }


    [Fact(DisplayName = "Tool enablement supports concurrent access")]
    public void ToolEnablementSupportsConcurrentAccess()
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
        Assert.True(service.IsEnabled(definition));
    }

    [Fact(DisplayName = "Tool enablement requires explicit proposal tool enablement")]
    public void ToolEnablementRequiresExplicitProposalToolEnablement()
    {
        var service = new AIToolEnablementService();
        var definition = new AIToolDefinition
        {
            Name = "workflow.propose",
            Mutability = AIToolMutability.Proposal,
            EnabledByDefault = true
        };

        Assert.False(service.IsEnabled(definition));

        service.Enable(definition.Name);

        Assert.True(service.IsEnabled(definition));
    }

    [Fact(DisplayName = "Tool enablement enables read-only tools by default")]
    public void ToolEnablementEnablesReadOnlyToolsByDefault()
    {
        var service = new AIToolEnablementService();
        var definition = new AIToolDefinition
        {
            Name = "workflow.inspect",
            Mutability = AIToolMutability.ReadOnly
        };

        Assert.True(service.IsEnabled(definition));
    }

    [Fact(DisplayName = "AI host allows context provider overrides on startup")]
    public async Task AIHostAllowsContextProviderOverridesOnStartup()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAIContextProvider>(new DuplicateContextProvider("WorkflowDefinition"));

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetServices<IHostedService>().OfType<AIContextProviderValidationHostedService>().Single();

        await validator.StartAsync(CancellationToken.None);
    }

    [Fact(DisplayName = "AI host validates scoped context providers from a startup scope")]
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

    private class ScopedAuditHandler : IAIAuditEventHandler
    {
        public static int RecordedCount { get; set; }

        public ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            RecordedCount++;
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

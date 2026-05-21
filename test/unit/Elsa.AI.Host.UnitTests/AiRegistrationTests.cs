using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Extensions;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    private class ScopedAuditHandler : IAiAuditEventHandler
    {
        public static int RecordedCount { get; set; }

        public ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            RecordedCount++;
            return ValueTask.CompletedTask;
        }
    }
}

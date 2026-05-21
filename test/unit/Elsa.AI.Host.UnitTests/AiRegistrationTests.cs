using Elsa.AI.Abstractions.Contracts;
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
}

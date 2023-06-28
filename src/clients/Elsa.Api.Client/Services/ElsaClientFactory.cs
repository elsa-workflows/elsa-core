using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.Client.Services;

/// <inheritdoc />
public class ElsaClientFactory : IElsaClientFactory
{
    /// <inheritdoc />
    public IElsaClient CreateClient(Action<ElsaClientOptions> configureOptions)
    {
        var services = new ServiceCollection().AddElsaClient(configureOptions).BuildServiceProvider();
        return services.GetRequiredService<IElsaClient>();
    }
}
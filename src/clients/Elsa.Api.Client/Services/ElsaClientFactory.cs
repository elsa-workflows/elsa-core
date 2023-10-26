using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.Client.Services;

/// <inheritdoc />
[PublicAPI]
public class ElsaClientFactory : IElsaClientFactory
{
    /// <inheritdoc />
    public IElsaClient CreateClient(Action<ElsaClientOptions> configureOptions, Action<ElsaClientBuilderOptions>? configureHttpClientBuilder = default)
    {
        var services = new ServiceCollection().AddElsaClient(configureOptions, configureHttpClientBuilder).BuildServiceProvider();
        return services.GetRequiredService<IElsaClient>();
    }
}
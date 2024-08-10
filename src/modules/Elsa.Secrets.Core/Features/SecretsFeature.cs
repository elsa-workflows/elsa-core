using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Secrets.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Features;

public class SecretsFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, ISecretProvider> _secretProviderFactory = _ => new NullSecretProvider();

    public SecretsFeature WithSecretsProvider(Func<IServiceProvider, ISecretProvider> secretProviderFactory)
    {
        _secretProviderFactory = secretProviderFactory;
        return this;
    }
    
    public override void Apply()
    {
        Services.AddScoped(_secretProviderFactory);
    }
}
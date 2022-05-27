using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Features.Abstractions;

public abstract class FeatureBase : IFeature
{
    protected FeatureBase(IModule module)
    {
        Module = module;
    }

    public IModule Module { get; }
    public IServiceCollection Services => Module.Services;

    public virtual void Configure()
    {
    }

    public virtual void ConfigureHostedServices()
    {
    }

    public virtual void Apply()
    {
    }
}
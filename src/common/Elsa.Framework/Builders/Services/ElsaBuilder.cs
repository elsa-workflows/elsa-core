using Elsa.Framework.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Builders;

public class ElsaBuilder
{
    private ICollection<IServiceConfigurator> Configurators { get; } = new List<IServiceConfigurator>();

    public void AddConfigurator(IServiceConfigurator configurator)
    {
        Configurators.Add(configurator);
    }

    public void Build(IServiceCollection services)
    {
        services.AddSingleton<ApplicationBlueprint>();
        
        foreach (var configurator in Configurators) 
            configurator.Configure(services);
    }
}
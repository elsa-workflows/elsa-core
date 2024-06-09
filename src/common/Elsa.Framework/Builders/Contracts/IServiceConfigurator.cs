using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Builders;

public interface IServiceConfigurator
{
    void Configure(IServiceCollection services);
}
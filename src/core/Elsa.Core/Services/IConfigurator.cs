using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services;

public interface IConfigurator
{
    void ConfigureServices(IServiceCollection services);
}
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells;

public interface IShellFeature
{
    void ConfigureServices(IServiceCollection services);
}
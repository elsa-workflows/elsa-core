using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells;

public abstract class ShellFeature : IShellFeature
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
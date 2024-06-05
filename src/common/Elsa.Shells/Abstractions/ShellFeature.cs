using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Shells;

public abstract class ShellFeature : IShellFeature
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual void Configure(IApplicationBuilder app)
    {
    }
}
using Elsa.Framework.Shells;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.AspNetCore;

public abstract class WebApplicationFeature : IWebApplicationFeature, IShellFeature
{
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual void Configure(IApplicationBuilder app)
    {
    }
}
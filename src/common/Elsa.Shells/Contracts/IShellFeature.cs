using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Shells;

public interface IShellFeature
{
    void ConfigureServices(IServiceCollection services);
    void Configure(IApplicationBuilder app);
}
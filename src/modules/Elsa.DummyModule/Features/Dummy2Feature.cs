using Elsa.DummyModule.Services;
using Elsa.Framework.Shells;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DummyModule.Features;

public class Dummy2Feature : ShellFeature
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDummyService, DummyService2>();
    }
}
using Elsa.Modules.Activities.Configurators;
using Elsa.Modules.Activities.Providers;
using Elsa.Modules.Activities.Resolvers;
using Elsa.Modules.Activities.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.Activities.Options;

public class ActivityOptions : IConfigurator
{
    public Func<IServiceProvider, IStandardInStreamProvider> StandardInStreamProvider { get; set; }  = _ => new StandardInStreamProvider(System.Console.In);
    public Func<IServiceProvider, IStandardOutStreamProvider> StandardOutStreamProvider { get; set; } = _ => new StandardOutStreamProvider(System.Console.Out);

    public ActivityOptions WithStandardInStreamProvider(Func<IServiceProvider, IStandardInStreamProvider> provider)
    {
        StandardInStreamProvider = provider;
        return this;
    }
    
    public ActivityOptions WithStandardOutStreamProvider(Func<IServiceProvider, IStandardOutStreamProvider> provider)
    {
        StandardOutStreamProvider = provider;
        return this;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IActivityNodeResolver, SwitchActivityNodeResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>()
            .AddSingleton(StandardInStreamProvider)
            .AddSingleton(StandardOutStreamProvider);
    }
}
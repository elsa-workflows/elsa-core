using System;
using System.Threading.Channels;
using Elsa.Features.Services;
using Elsa.Jobs.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Extensions;

public static class ModuleExtensions
{
    public static IModule UseJobs(this IModule module, Action<JobsFeature>? configure = default )
    {
        module.Configure(configure);
        return module;
    }
    
    public static IServiceCollection CreateChannel<T>(this IServiceCollection services) =>
        services
            .AddSingleton(CreateChannel<T>())
            .AddSingleton(CreateChannelReader<T>)
            .AddSingleton(CreateChannelWriter<T>);
    
    private static Channel<T> CreateChannel<T>() => Channel.CreateUnbounded<T>(new UnboundedChannelOptions());
    private static ChannelReader<T> CreateChannelReader<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Reader;
    private static ChannelWriter<T> CreateChannelWriter<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Writer;
}
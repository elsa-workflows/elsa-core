using Elsa.Logging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Extensions;

public static class LogSinkServiceCollectionExtensions
{
    public static IServiceCollection AddLogSink<T>(this IServiceCollection services) where T: class, ILogSink
    {
        return services.AddSingleton<ILogSink, T>();
    }
}
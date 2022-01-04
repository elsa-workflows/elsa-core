

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpActivityServices(this IServiceCollection services)
    {
        return services
            .AddHttpContextAccessor();
    }
}
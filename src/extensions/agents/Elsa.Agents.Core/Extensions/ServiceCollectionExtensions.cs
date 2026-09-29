using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSkillsProvider<T>(this IServiceCollection services) where T: class, ISkillsProvider
    {
        return services.AddScoped<ISkillsProvider, T>();
    }
}
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.Hangfire.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers both Hangfire and Elsa specific services.
    /// If you register Hangfire yourself, use <see cref="ConfigureHangfireModule"/>
    /// </summary>
    public static IServiceCollection AddHangfireAndModule(this IServiceCollection services)
    {
        
    }
}
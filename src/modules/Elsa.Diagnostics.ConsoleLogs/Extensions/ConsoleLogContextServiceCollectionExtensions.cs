using ConsoleLogStreaming.Core;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

internal static class ConsoleLogContextServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleLogContextServices(this IServiceCollection services)
    {
        services.TryAddSingleton(_ => ConsoleLogContextAccessor.Instance);
        services.TryAddSingleton<IConsoleLogContextAccessor>(sp => sp.GetRequiredService<ConsoleLogContextAccessor>());
        services.TryAddSingleton<IConsoleLogMetadataAccessor>(sp => sp.GetRequiredService<ConsoleLogContextAccessor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IWorkflowExecutionPipelineContributor, ConsoleLogWorkflowExecutionPipelineContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IActivityExecutionPipelineContributor, ConsoleLogActivityExecutionPipelineContributor>());
        return services;
    }
}

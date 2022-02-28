using Elsa.DataMasking.Core.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DataMasking.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataMasking(this IServiceCollection services)
    {
        return services.AddNotificationHandlersFrom<ApplyWorkflowJournalFilters>();
    }
}
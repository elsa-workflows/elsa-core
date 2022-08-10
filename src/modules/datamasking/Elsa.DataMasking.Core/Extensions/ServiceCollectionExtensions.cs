using Autofac;
using Elsa.DataMasking.Core.Handlers;

namespace Elsa.DataMasking.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static ContainerBuilder AddDataMasking(this ContainerBuilder services)
    {
        return services.AddNotificationHandlersFrom<ApplyWorkflowJournalFilters>();
    }
}
using Elsa.Activities.Entity;
using Elsa.Activities.Entity.Triggers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityActivities(this IServiceCollection services) =>
            services
                .AddActivity<EntityChanged>()
                .AddTriggerProvider<EntityChangedTriggerProvider>();
    }
}

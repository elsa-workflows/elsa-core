using Elsa.Activities.Entity;
using Elsa.Activities.Entity.Bookmarks;
using Elsa.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddEntityActivities(this ElsaOptionsBuilder options)
        {
            options.AddActivity<EntityChanged>();
            options.Services.AddBookmarkProvider<EntityChangedWorkflowTriggerProvider>();
            return options;
        }
    }
}
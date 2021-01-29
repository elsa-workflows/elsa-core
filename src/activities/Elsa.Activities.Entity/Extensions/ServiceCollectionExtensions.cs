using Elsa;
using Elsa.Activities.Entity;
using Elsa.Activities.Entity.Bookmarks;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddEntityActivities(this ElsaOptions options)
        {
            options.AddActivity<EntityChanged>();
            options.Services.AddBookmarkProvider<EntityChangedWorkflowTriggerProvider>();
            return options;
        }
    }
}
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.EntityChanged
{
    /// <summary>
    /// Demonstrates the use of the EntityChanged activity.
    /// </summary>
    internal class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options.AddConsoleActivities().AddEntityActivities().AddWorkflow<EntityChangedWorkflow>())
                .AddSingleton<SomeRepository>()
                
                .BuildServiceProvider();

            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Resolve a repository.
            var repository = services.GetRequiredService<SomeRepository>();
            var entity1 = new Entity { Id = "entity-1", Title = "Some Title" };
            var entity2 = new Entity { Id = "entity-2", Title = "Some Title" };
            
            // Add the first entity to the repository to trigger the workflow.
            await repository.AddAsync(entity1);
            
            // Delete the second entity from the repository. Notice that it will not trigger any workflows, because the entity-1 workflow is correlated to entity-1.
            await repository.DeleteAsync(entity2);
            
            // Delete the first  entity from the repository. Notice that it will resume the first workflow, since it's blocking on the Deleted change action for entity-1.
            await repository.DeleteAsync(entity1);
        }
    }
}
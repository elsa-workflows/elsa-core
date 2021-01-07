using System;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Persistence.EntityFramework
{
    class Program
    {
        private static async Task Main()
        {
            // Create a service container with Elsa services.
            var services = new ServiceCollection()
                .AddElsa(options => options
                    // Configure Elsa to use the Entity Framework Core persistence provider using Sqlite.
                    .UseEntityFrameworkPersistence(ef =>
                    {
                        ef.UseSqlite("Data Source=elsa.db;Cache=Shared", db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
                    }, true)
                    .AddConsoleActivities()
                    .AddWorkflow<HelloWorld>())
                .AddAutoMapperProfiles<Program>()
                .BuildServiceProvider();
            
            // Run startup actions (not needed when registering Elsa with a Host).
            var startupRunner = services.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();

            // Get a workflow runner.
            var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

            // Run the workflow.
            var workflowInstance = await workflowRunner.RunWorkflowAsync<HelloWorld>();

            // Get a reference to the workflow instance store.
            var store = services.GetRequiredService<IWorkflowInstanceStore>();

            // Count the number of workflow instances of HelloWorld.
            var count = await store.CountAsync(new WorkflowInstanceDefinitionIdSpecification(nameof(HelloWorld)));

            Console.WriteLine(count);
            
            var loadedWorkflowInstance = await store.FindByIdAsync(workflowInstance.Id);
            Console.WriteLine(loadedWorkflowInstance);
        }
    }
}
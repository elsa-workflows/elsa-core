using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.EntityFrameworkCore;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Sample22
{
    /// <summary>
    /// A simple demonstration of using Entity Framework Core persistence providers with SQL Server.
    /// To run the EF migration, run the command: `dotnet ef database update` within the context of the "Elsa.Persistence.EntityFrameworkCore" project root.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var services = BuildServices();

            // Create a workflow definition.
            var registry = services.GetService<IWorkflowRegistry>();
            var workflowDefinition = await registry.GetWorkflowDefinitionAsync<HelloWorldWorkflow>();

            // Mark this definition as the "latest" version.
            workflowDefinition.IsLatest = true;
            workflowDefinition.Version = 1;

            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ElsaContext>();

            // Ensure DB exists.
            await dbContext.Database.EnsureCreatedAsync();

            // Execute the workflow.
            var runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
            var executionContext = await runner.RunAsync(workflowDefinition);

            // Persist the workflow instance.
            var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflowInstance = executionContext.Workflow.ToInstance();
            await instanceStore.SaveAsync(workflowInstance);

            // Flush to DB.
            await dbContext.SaveChangesAsync();
        }

        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddElsa(
                    x => x.AddEntityFrameworkStores(
                        options => options
                            .UseSqlServer(@"Server=localhost;Database=Elsa;User=sa;Password=Secret_password123!;")))
                .AddConsoleActivities()
                .AddWorkflow<HelloWorldWorkflow>()
                .BuildServiceProvider();
        }
    }
}
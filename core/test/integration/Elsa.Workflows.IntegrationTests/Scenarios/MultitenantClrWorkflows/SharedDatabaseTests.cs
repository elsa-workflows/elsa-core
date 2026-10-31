using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.MultitenantClrWorkflows;

public class SharedDatabaseTests(ITestOutputHelper testOutputHelper)
{
    [Fact(DisplayName = "A second tenant sharing the database does not save a CLR workflow the first tenant already stored, but still indexes its triggers")]
    public async Task SecondTenant_DoesNotSaveStoredClrWorkflow_ButIndexesTriggers()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-multitenant-clr-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=30;Pooling=False";

        try
        {
            await using var services = (ServiceProvider)new TestApplicationBuilder(testOutputHelper)
                .ConfigureElsa(elsa => elsa
                    .UseWorkflowManagement(management => management.UseWorkflowDefinitions(definitions =>
                        definitions.UseEntityFrameworkCore(persistence => persistence.UseSqlite(connectionString)))))
                .AddWorkflow<RichWorkflow>()
                .Build();
            var tenantAccessor = (DefaultTenantAccessor)services.GetRequiredService<ITenantAccessor>();

            await using (var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            await ActivateTenantAsync(services, tenantAccessor, "tenant-1");
            var rowAfterFirstTenant = await FindRowAsync(services);
            await Task.Delay(50);

            await ActivateTenantAsync(services, tenantAccessor, "tenant-2");

            var rowAfterSecondTenant = await FindRowAsync(services);
            Assert.Equal(rowAfterFirstTenant.CreatedAt, rowAfterSecondTenant.CreatedAt);

            using var _ = tenantAccessor.PushContext(new() { Id = "tenant-2", Name = "tenant-2" });
            using var scope = services.CreateScope();
            var triggers = await scope.ServiceProvider.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { WorkflowDefinitionId = nameof(RichWorkflow) });
            Assert.Contains(triggers, x => x.TenantId == "tenant-2");
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static async Task ActivateTenantAsync(IServiceProvider services, DefaultTenantAccessor tenantAccessor, string tenantId)
    {
        using var _ = tenantAccessor.PushContext(new() { Id = tenantId, Name = tenantId });
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
    }

    private static async Task<WorkflowDefinition> FindRowAsync(IServiceProvider services)
    {
        await using var dbContext = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        return await dbContext.WorkflowDefinitions.AsNoTracking().IgnoreQueryFilters().SingleAsync(x => x.DefinitionId == nameof(RichWorkflow));
    }

    private class RichWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithVariable("Greeting", "hello");
            builder.WithInput<string>("Name", "The name to greet");
            builder.WithOutput<string>("Result", "The greeting");
            builder.Outcomes = ["Done"];
            builder.WithCustomProperty("Category", "Test");
            builder.Root = new Event("multitenant-clr-rich-event") { CanStartWorkflow = true };
        }
    }
}

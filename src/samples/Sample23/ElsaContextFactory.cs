using Elsa;
using Elsa.Persistence.EntityFrameworkCore.CustomSchema;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Sample23
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            // Migrations added to Sample23.CustomMigration with the following Powershell command
            // Add-Migration InitialCreate -context ElsaContext -Project Sample23.CustomMigration
            var services = new ServiceCollection();
            ElsaBuilder elsaBuilder = new ElsaBuilder(services);
            elsaBuilder.AddCustomSchema("elsa");

            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();

            optionsBuilder.UseSqlite(
                @"Data Source=c:\data\elsa.entity-framework-core.db;Cache=Shared",
                x =>
                {
                    x.AddCustomSchemaModelSupport(optionsBuilder, elsaBuilder.Services);
                    x.MigrationsAssembly(typeof(Program).Assembly.FullName);
                    x.MigrationsHistoryTableWithSchema(optionsBuilder);
                });

            return new ElsaContext(optionsBuilder.Options);
        }
    }
}
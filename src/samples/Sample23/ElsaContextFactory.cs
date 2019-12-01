using Elsa.Persistence.EntityFrameworkCore;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Sample23.CustomMigration;

namespace Sample23
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            // Migrations added to Sample23.CustomMigration with the following Powershell command
            // Add-Migration InitialCreate -context ElsaContext -Project Sample23.CustomMigration
            var services = new ServiceCollection();
            services.AddCustomSchemaSupport("elsa");

            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();

            optionsBuilder.UseSqlite(
                @"Data Source=c:\data\elsa.entity-framework-core.db;Cache=Shared",
                x =>
                {
                    x.MigrationsAssembly(typeof(MigrateStub).Assembly.FullName);
                    using (var scope = services.BuildServiceProvider().CreateScope())
                    {
                        IDbContextCustomSchema dbContextCustomSchema = scope.ServiceProvider.GetService<IDbContextCustomSchema>();
                        if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
                        {
                            x.MigrationsHistoryTable(dbContextCustomSchema.CustomMigrationsHistoryTableName, dbContextCustomSchema.CustomDefaultSchema);
                        }
                    }
                });

            optionsBuilder.ReplaceService<IModelCacheKeyFactory, CustomSchemaModelCacheKeyFactory>();

            return new ElsaContext(optionsBuilder.Options);
        }
    }
}
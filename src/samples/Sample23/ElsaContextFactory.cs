using Elsa.Persistence.EntityFrameworkCore;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sample23.CustomMigration;

namespace Sample23
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();
            string customDefaultSchema = "elsa";

            optionsBuilder.UseSqlite(
                @"Data Source=c:\data\elsa.entity-framework-core.db;Cache=Shared",
                x =>
                {
                    x.MigrationsAssembly(typeof(MigrateStub).Assembly.FullName);
                    x.MigrationsHistoryTable("__EFMigrationsHistory", customDefaultSchema);
                });

            optionsBuilder.ReplaceService<IModelCacheKeyFactory, CustomSchemaModelCacheKeyFactory>();

            return new ElsaContext(optionsBuilder.Options);
        }
    }
}
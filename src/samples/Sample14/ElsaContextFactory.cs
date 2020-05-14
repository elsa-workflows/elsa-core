using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sample14
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();
            
            optionsBuilder.UseSqlite(
                @"Data Source=c:\data\elsa.entity-framework-core.db;Cache=Shared", 
                x => x.MigrationsAssembly(typeof(Program).Assembly.FullName));
            
            return new ElsaContext(optionsBuilder.Options);
        }
    }
}
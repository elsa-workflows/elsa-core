using Elsa.Persistence.EntityFrameworkCore;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sample16
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();

            optionsBuilder.UseSqlite(
                @"Data Source=C:\Dev\elsa-core\src\samples\Sample16\elsa.sample16.db;Cache=Shared",
                x => x.MigrationsAssembly(typeof(Program).Assembly.FullName));

            return new ElsaContext(optionsBuilder.Options);
        }
    }
}
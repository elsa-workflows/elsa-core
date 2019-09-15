using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();

            optionsBuilder.UseSqlite(
                "Data Source=c:\\data\\elsa.db;Cache=Shared",
                x => x.MigrationsAssembly(typeof(ElsaContextFactory).Assembly.FullName)
            );

            return new ElsaContext(optionsBuilder.Options);
        }
    }
}
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public abstract class DesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : DbContext
    {
        public TDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
            var migrationAssembly = typeof(TDbContext).Assembly.FullName;
            var connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING");

            if(connectionString == null)
            {
                var providerName = typeof(TDbContext).Name.Replace("Context", "");
                throw new InvalidOperationException($"Set the EF_CONNECTIONSTRING environment variable to a valid {providerName} connection string.");
            }

            optionsBuilder.UseSqlite(
                connectionString,
                x => x.MigrationsAssembly(migrationAssembly)
            );

            return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options);
        }
    }
}
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore
{
    public class DesignTimeElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();
            var migrationAssembly = typeof(DesignTimeElsaContextFactory).Assembly.FullName;
            var provider = Environment.GetEnvironmentVariable("EF_PROVIDER") ?? "Sqlite";
            var connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING") ?? "Data Source=c:\\data\\elsa.db;Cache=Shared";

            switch (provider)
            {
                case "Sqlite":
                    optionsBuilder.UseSqlite(
                        connectionString,
                        x => x.MigrationsAssembly(migrationAssembly)
                    );
                    break;
                case "SqlServer":
                    optionsBuilder.UseSqlServer(
                        connectionString,
                        x => x.MigrationsAssembly(migrationAssembly)    
                    );
                    break;
                default:
                    throw new NotSupportedException("The specified provider is not supported.");
            }
            
            return new ElsaContext(optionsBuilder.Options);
        }
    }
}
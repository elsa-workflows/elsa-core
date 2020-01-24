using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class PostgreSqlContextFactory : IDesignTimeDbContextFactory<PostgreSqlContext>
    {
        public PostgreSqlContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlContext>();
            var migrationAssembly = typeof(PostgreSqlContext).Assembly.FullName;
            var connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING");

            if (connectionString == null)
                throw new InvalidOperationException(@"Set the EF_CONNECTIONSTRING environment variable to a valid PostgreSql connection string. E.g. SET EF_CONNECTIONSTRING=Server=localhost;Database=Elsa;Port=5432;User Id=postgres;Password=Secret_password123!;");

            optionsBuilder.UseNpgsql(
                connectionString,
                x => x.MigrationsAssembly(migrationAssembly)
            );

            return new PostgreSqlContext(optionsBuilder.Options);
        }
    }
}
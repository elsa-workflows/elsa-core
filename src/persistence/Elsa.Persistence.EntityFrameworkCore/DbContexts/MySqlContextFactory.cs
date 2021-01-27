using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class MySqlContextFactory : IDesignTimeDbContextFactory<MySqlContext>
    {
        public MySqlContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MySqlContext>();
            var migrationAssembly = typeof(MySqlContext).Assembly.FullName;
            var connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING");

            if (connectionString == null)
                throw new InvalidOperationException("Set the EF_CONNECTIONSTRING environment variable to a valid MySQL connection string. E.g. SET EF_CONNECTIONSTRING=Server=localhost;Database=Elsa;User=sa;Password=Secret_password123!;");

            optionsBuilder.UseMySql(
                new MySqlServerVersion(ServerVersion.AutoDetect(connectionString)),
                x => x.MigrationsAssembly(migrationAssembly)
            );

            return new MySqlContext(optionsBuilder.Options);
        }
    }
}
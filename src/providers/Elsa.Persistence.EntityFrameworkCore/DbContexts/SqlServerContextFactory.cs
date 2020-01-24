using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class SqlServerContextFactory : IDesignTimeDbContextFactory<SqlServerContext>
    {
        public SqlServerContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqlServerContext>();
            var migrationAssembly = typeof(SqlServerContext).Assembly.FullName;
            var connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING");

            if(connectionString == null)
                throw new InvalidOperationException("Set the EF_CONNECTIONSTRING environment variable to a valid SQL Server connection string. E.g. SET EF_CONNECTIONSTRING=Server=localhost;Database=Elsa;User=sa;Password=Secret_password123!;");
            
            optionsBuilder.UseSqlServer(
                connectionString,
                x => x.MigrationsAssembly(migrationAssembly)
            );

            return new SqlServerContext(optionsBuilder.Options);
        }
    }
}
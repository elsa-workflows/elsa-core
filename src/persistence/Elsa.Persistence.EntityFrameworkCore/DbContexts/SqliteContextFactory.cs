using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class SqliteContextFactory : IDesignTimeDbContextFactory<SqliteContext>
    {
        public SqliteContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteContext>();
            var migrationAssembly = typeof(SqliteContext).Assembly.FullName;
            var connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING");

            if(connectionString == null)
                throw new InvalidOperationException(@"Set the EF_CONNECTIONSTRING environment variable to a valid SQLite connection string. E.g. SET EF_CONNECTIONSTRING=Data Source=c:\data\elsa.db;Cache=Shared;");

            optionsBuilder.UseSqlite(
                connectionString,
                x => x.MigrationsAssembly(migrationAssembly)
            );

            return new SqliteContext(optionsBuilder.Options);
        }
    }
}
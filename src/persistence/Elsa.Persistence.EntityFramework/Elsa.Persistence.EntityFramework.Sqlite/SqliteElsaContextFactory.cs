using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFramework.Sqlite
{
    public class SqliteElsaContextFactory : IDesignTimeDbContextFactory<SqliteElsaContext>
    {
        public SqliteElsaContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SqliteElsaContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.db;Cache=Shared";
            builder.UseSqlite(connectionString);
            return new SqliteElsaContext(builder.Options);
        }
    }
}
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFramework.SqlServer
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<SqlServerElsaContext>
    {
        public SqlServerElsaContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SqlServerElsaContext>();
            var connectionString = args.Any() ? args[0] : throw new InvalidOperationException("Please specify a connection string. E.g. dotnet ef database update -- \"Server=Local;Database=elsa\"");
            builder.UseSqlServer(connectionString);
            return new SqlServerElsaContext(builder.Options);
        }
    }
}
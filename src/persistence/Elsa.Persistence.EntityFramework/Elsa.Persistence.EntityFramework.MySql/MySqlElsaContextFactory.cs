using System;
using System.Linq;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Elsa.Persistence.EntityFramework.MySql
{
    public class MySqlElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ElsaContext>();
            var connectionString = args.Any() ? args[0] : throw new InvalidOperationException("Please specify a connection string. E.g. dotnet ef database update -- \"Server=localhost;Port=3306;Database=elsa;User=root;Password=password\"");
            
            builder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(args[0]),
                db => db.MigrationsAssembly(typeof(MySqlElsaContextFactory).Assembly.GetName().Name));
            
            return new ElsaContext(builder.Options);
        }
    }
}
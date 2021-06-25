using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class OracleContextFactory:IDesignTimeDbContextFactory<OracleContext>
    {
        public OracleContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OracleContext>();
            var migrationAssembly = typeof(OracleContext).Assembly.FullName;
            var connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING");

            if (connectionString == null)
                throw new InvalidOperationException("Set the EF_CONNECTIONSTRING environment variable to a valid Oracle connection string. E.g. SET EF_CONNECTIONSTRING=Data Source=127.0.0.1;port=1521;user id=postgres;password=password;;");

            optionsBuilder.UseOracle(
                connectionString,
                x => x.MigrationsAssembly(migrationAssembly)
            );

            return new OracleContext(optionsBuilder.Options);
        }

    }
}

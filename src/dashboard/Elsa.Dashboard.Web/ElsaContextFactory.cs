using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elsa.Dashboard.Web
{
    public class ElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ElsaContext>();

            optionsBuilder.UseSqlite(
                @"Data Source=C:\\Dev\\elsa-core\\src\\samples\\DocumentApproval\\elsa.sample16.db;Cache=Shared",
                x => x.MigrationsAssembly(typeof(Program).Assembly.FullName));

            return new ElsaContext(optionsBuilder.Options);
        }


    }
}

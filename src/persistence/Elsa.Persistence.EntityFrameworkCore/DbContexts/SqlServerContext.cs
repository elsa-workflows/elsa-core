using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class SqlServerContext : ElsaContext
    {
        public SqlServerContext(DbContextOptions options) : base(options)
        {
        }
    }
}
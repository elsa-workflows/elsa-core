using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class PostgreSqlContext : ElsaContext
    {
        public PostgreSqlContext(DbContextOptions options) : base(options)
        {
        }
    }
}
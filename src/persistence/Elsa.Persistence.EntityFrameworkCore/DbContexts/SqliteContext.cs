using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class SqliteContext : ElsaContext
    {
        public SqliteContext(DbContextOptions<ElsaContext> options) : base(options)
        {
        }
    }
}
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class SqliteContext : ElsaContext
    {
        public SqliteContext(DbContextOptions<SqliteContext> options) : base(options)
        {
        }
    }
}
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class SqliteContext : ElsaContext
    {
        public SqliteContext(DbContextOptions options) : base(options)
        {
        }
    }
}
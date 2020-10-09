using Elsa.Persistence.Core.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.SqlLite
{
    public class SqliteContext : ElsaContext
    {
        public SqliteContext(DbContextOptions<SqliteContext> options) : base(options)
        {
        }
    }
}
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Sqlite
{
    public class SqliteElsaContext : ElsaContext
    {
        public SqliteElsaContext(DbContextOptions<SqliteElsaContext> options) : base(options)
        {
        }
    }
}
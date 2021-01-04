using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.SqlServer
{
    public class SqlServerElsaContext : ElsaContext
    {
        public SqlServerElsaContext(DbContextOptions<SqlServerElsaContext> options) : base(options)
        {
        }
    }
}
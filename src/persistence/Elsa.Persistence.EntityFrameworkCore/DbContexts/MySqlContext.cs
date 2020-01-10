using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class MySqlContext : ElsaContext
    {
        public MySqlContext(DbContextOptions<ElsaContext> options) : base(options)
        {
        }
    }
}
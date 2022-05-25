using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.DbContexts;

public class SqliteElsaDbContext : ElsaDbContext
{
    public SqliteElsaDbContext(DbContextOptions options) : base(options)
    {
    }
}
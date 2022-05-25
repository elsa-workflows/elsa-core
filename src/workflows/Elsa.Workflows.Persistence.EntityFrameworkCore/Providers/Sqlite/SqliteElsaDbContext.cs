using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Providers.Sqlite;

public class SqliteElsaDbContext : ElsaDbContext
{
    public SqliteElsaDbContext(DbContextOptions options) : base(options)
    {
    }
}
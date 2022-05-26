using Microsoft.EntityFrameworkCore;

namespace Elsa.Labels.EntityFrameworkCore.Providers.Sqlite;

public class SqliteLabelsDbContext : LabelsDbContext
{
    public SqliteLabelsDbContext(DbContextOptions options) : base(options)
    {
    }
}
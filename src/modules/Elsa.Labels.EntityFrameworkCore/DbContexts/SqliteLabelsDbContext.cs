using Microsoft.EntityFrameworkCore;

namespace Elsa.Labels.EntityFrameworkCore.DbContexts;

public class SqliteLabelsDbContext : LabelsDbContext
{
    public SqliteLabelsDbContext(DbContextOptions options) : base(options)
    {
    }
}
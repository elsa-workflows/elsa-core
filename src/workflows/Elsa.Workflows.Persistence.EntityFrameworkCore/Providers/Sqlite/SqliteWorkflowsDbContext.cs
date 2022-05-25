using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Providers.Sqlite;

public class SqliteWorkflowsDbContext : WorkflowsDbContext
{
    public SqliteWorkflowsDbContext(DbContextOptions options) : base(options)
    {
    }
}
using Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.WorkflowSink;

// ReSharper disable once UnusedType.Global
public class SqliteDesignTimeWorkflowSinkElsaDbContextFactory : SqliteDesignTimeDbContextFactoryBase<WorkflowSinkElsaDbContext>
{
}
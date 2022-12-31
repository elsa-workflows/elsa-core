using Elsa.EntityFrameworkCore.Sqlite.Abstractions;
using Elsa.EntityFrameworkCore.Modules.WorkflowSink;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.WorkflowSink;

// ReSharper disable once UnusedType.Global
public class SqliteDesignTimeWorkflowSinkElsaDbContextFactory : SqliteDesignTimeDbContextFactoryBase<WorkflowSinkElsaDbContext>
{
}
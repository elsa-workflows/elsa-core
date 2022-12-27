using Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.WorkflowSink;

// ReSharper disable once UnusedType.Global
public class DesignTimeWorkflowSinkElsaDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<WorkflowSinkElsaDbContext>
{
}
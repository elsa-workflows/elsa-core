using Elsa.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.WorkflowSink;

// ReSharper disable once UnusedType.Global
public class DesignTimeWorkflowSinkElsaDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<WorkflowSinkElsaDbContext>
{
}
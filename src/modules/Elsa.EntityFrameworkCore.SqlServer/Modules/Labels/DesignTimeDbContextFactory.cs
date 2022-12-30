using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
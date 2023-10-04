using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Labels;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
using Elsa.Persistence.EntityFrameworkCore.Modules.Labels;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqlServerDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
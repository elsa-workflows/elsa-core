using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.PostgreSql.Abstractions;

namespace Elsa.EntityFrameworkCore.PostgreSql.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : PostgreSqlDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
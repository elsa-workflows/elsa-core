using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Labels;

namespace Elsa.EntityFrameworkCore.PostgreSql.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : PostgreSqlDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
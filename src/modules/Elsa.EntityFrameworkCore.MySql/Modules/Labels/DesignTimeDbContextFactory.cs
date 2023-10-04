using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Labels;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : MySqlDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
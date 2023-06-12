using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.MySql.Abstractions;

namespace Elsa.EntityFrameworkCore.MySql.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : MySqlDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
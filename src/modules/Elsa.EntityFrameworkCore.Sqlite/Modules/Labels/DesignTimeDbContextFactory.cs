using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
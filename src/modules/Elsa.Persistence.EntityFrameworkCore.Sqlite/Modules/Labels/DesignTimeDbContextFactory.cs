using Elsa.Persistence.EntityFrameworkCore.Modules.Labels;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Abstractions;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Labels;

// ReSharper disable once UnusedType.Global
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<LabelsDbContext>
{
}
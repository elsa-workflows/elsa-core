using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Labels;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Labels;

/// <summary>
/// The design-time factory for the <see cref="LabelsElsaDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeDbContextFactory : SqliteDesignTimeDbContextFactoryBase<LabelsElsaDbContext>
{
}
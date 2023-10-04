using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Alterations;


/// <summary>
/// The design-time factory for the <see cref="AlterationsDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeAlterationsDbContextFactory : SqliteDesignTimeDbContextFactoryBase<AlterationsDbContext>
{
}
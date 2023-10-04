using Elsa.EntityFrameworkCore.Common.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Alterations.Persistence.EntityFrameworkCore.Sqlite;


/// <summary>
/// The design-time factory for the <see cref="AlterationsDbContext"/>.
/// </summary>
[PublicAPI]
public class DesignTimeAlterationsDbContextFactory : SqliteDesignTimeDbContextFactoryBase<AlterationsDbContext>
{
}
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Labels.EntityFrameworkCore.Providers.Sqlite
{
    // ReSharper disable once UnusedType.Global
    public class SqliteDesignTimeDbElsaContextFactory : SqliteDesignTimeDbContextFactory<SqliteLabelsDbContext>
    {
    }
}
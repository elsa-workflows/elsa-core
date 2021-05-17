using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    internal static class PostgresDatabaseFacadeExtensions
    {
        public static bool IsPostgres(this DatabaseFacade database) => database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
    }
}
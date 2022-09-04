using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions;

internal static class DatabaseFacadeExtensions
{
    public static bool IsMySql(this DatabaseFacade database) => database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
    public static bool IsOracle(this DatabaseFacade database) => database.ProviderName == "Oracle.EntityFrameworkCore";
    public static bool IsPostgres(this DatabaseFacade database) => database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
}
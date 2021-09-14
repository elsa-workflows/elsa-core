using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    internal static class OracleDatabaseFacadeExtensions
    {
        public static bool IsOracle(this DatabaseFacade database) => database.ProviderName == "Oracle.EntityFrameworkCore";
    }
}
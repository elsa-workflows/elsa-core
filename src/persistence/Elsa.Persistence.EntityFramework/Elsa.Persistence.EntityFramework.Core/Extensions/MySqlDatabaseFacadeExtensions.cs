using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    internal static class MySqlDatabaseFacadeExtensions
    {
        public static bool IsMySql(this DatabaseFacade database) => database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
    }
}

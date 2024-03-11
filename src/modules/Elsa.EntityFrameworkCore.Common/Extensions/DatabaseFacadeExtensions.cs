using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for the DatabaseFacade class.
/// </summary>
public static class DatabaseFacadeExtensions
{
    /// <summary>
    /// Returns true if the database provider is MySql.
    /// </summary>
    public static bool IsMySql(this DatabaseFacade database) => database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
    
    /// <summary>
    /// Returns true if the database provider is Oracle.
    /// </summary>
    public static bool IsOracle(this DatabaseFacade database) => database.ProviderName == "Oracle.EntityFrameworkCore";
    
    /// <summary>
    /// Returns true if the database provider is Postgres.
    /// </summary>
    public static bool IsPostgres(this DatabaseFacade database) => database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
}
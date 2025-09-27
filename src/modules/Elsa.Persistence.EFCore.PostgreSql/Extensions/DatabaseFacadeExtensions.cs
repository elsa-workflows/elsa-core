using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Extension methods for the DatabaseFacade class.
/// </summary>
public static class DatabaseFacadeExtensions
{
    /// <summary>
    /// Returns true if the database provider is Postgres.
    /// </summary>
    public static bool IsPostgres(this DatabaseFacade database) => database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
}
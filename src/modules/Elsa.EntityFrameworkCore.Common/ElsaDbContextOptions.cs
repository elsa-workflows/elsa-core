using Elsa.EntityFrameworkCore.Common.Abstractions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// Provides options for configuring Elsa's Entity Framework Core integration.
/// </summary>
[PublicAPI]
public class ElsaDbContextOptions
{
    /// <summary>
    /// The schema used by Elsa.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// The table used to store the migrations history.
    /// </summary>
    public string? MigrationsHistoryTableName { get; set; }

    /// <summary>
    /// The assembly name containing the migrations.
    /// </summary>
    public string? MigrationsAssemblyName { get; set; }

    /// <summary>
    /// Gets or sets a delegate that can be used to add additionnal entity configuration to Elsa's DbContext.
    /// </summary>
    public Action<ModelBuilder, IServiceProvider>? AdditionnalEntityConfigurations { get; set; }

    /// <summary>
    /// Get or sets the DbContextStrategies to apply on the DbContext
    /// </summary>
    public IEnumerable<IDbContextStrategy> dbContextStrategies { get; set; }
}

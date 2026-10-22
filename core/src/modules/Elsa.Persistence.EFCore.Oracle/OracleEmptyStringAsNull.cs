using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.Persistence.EFCore.Oracle;

/// <summary>
/// Maps the default tenant id <c>""</c> to <c>NULL</c> on Oracle and back again.
/// </summary>
/// <remarks>
/// Oracle stores <c>''</c> as <c>NULL</c>. A write of the default tenant therefore becomes a null bind,
/// and a comparison of <c>TenantId == ""</c> must translate to <c>IS NULL</c> rather than
/// <c>TenantId = ''</c> (which never matches). The converter does both.
/// </remarks>
internal static class OracleEmptyStringAsNull
{
    public static readonly ValueConverter<string, string?> Converter = new(
        value => string.IsNullOrEmpty(value) ? null : value,
        value => value ?? string.Empty);

    /// <summary>
    /// Marks every string <c>TenantId</c> property optional and applies <see cref="Converter"/>.
    /// Must run after any Oracle-only key rewrite that removes <c>TenantId</c> from a primary key:
    /// EF Core rejects <c>IsRequired(false)</c> on a key property.
    /// </summary>
    public static void ApplyToTenantIdProperties(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var property = entityType.FindProperty("TenantId");
            if (property is null || property.ClrType != typeof(string) || property.IsPrimaryKey())
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property<string>("TenantId")
                .IsRequired(false)
                .HasConversion(Converter);
        }
    }
}

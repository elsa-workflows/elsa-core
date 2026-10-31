using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.Persistence.EFCore.Oracle;

/// <summary>
/// Maps the default tenant id <c>""</c> to <c>NULL</c> on Oracle writes.
/// </summary>
/// <remarks>
/// Oracle stores <c>''</c> as <c>NULL</c>. A write of the default tenant therefore becomes a null bind.
/// A constant comparison of <c>TenantId == ""</c> translates to <c>IS NULL</c> rather than
/// <c>TenantId = ''</c> (which never matches). EF Core does not invoke the converter for database
/// <c>NULL</c>, so a materialized default-tenant <c>TenantId</c> stays <c>null</c>.
/// </remarks>
internal static class OracleEmptyStringAsNull
{
    public static readonly ValueConverter<string, string?> Converter = new(
        value => string.IsNullOrEmpty(value) ? null : value,
        value => value ?? string.Empty);
}

using System.Data;
using Dapper;

namespace Elsa.Persistence.Dapper.TypeHandlers;

/// <summary>
/// Maps provider-native timestamps and SQLite timestamp strings to <see cref="DateTimeOffset"/>.
/// Dapper registers handlers process-wide, so this handler accepts the forms used by each provider.
/// </summary>
internal sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) => parameter.Value = value;

    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dto => dto,
        DateTime dt => dt.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
            : new DateTimeOffset(dt),
        string s => DateTimeOffset.Parse(s),
        _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset.")
    };
}

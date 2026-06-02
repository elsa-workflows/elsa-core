using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Represents a typed value used by portable document queries.
/// </summary>
public sealed record DocumentQueryValue
{
    private DocumentQueryValue(StorageFieldType type, string? textValue, decimal? numberValue, bool? booleanValue, DateTimeOffset? dateTimeValue)
    {
        Type = type;
        TextValue = textValue;
        NumberValue = numberValue;
        BooleanValue = booleanValue;
        DateTimeValue = dateTimeValue;
    }

    public StorageFieldType Type { get; }

    public string? TextValue { get; }

    public decimal? NumberValue { get; }

    public bool? BooleanValue { get; }

    public DateTimeOffset? DateTimeValue { get; }

    public static DocumentQueryValue String(string value) => new(StorageFieldType.String, RequireText(value, nameof(value)), null, null, null);

    public static DocumentQueryValue Guid(Guid value) => new(StorageFieldType.Guid, value.ToString("D"), null, null, null);

    public static DocumentQueryValue Json(string value) => new(StorageFieldType.Json, RequireText(value, nameof(value)), null, null, null);

    public static DocumentQueryValue Binary(string base64Value) => new(StorageFieldType.Binary, RequireText(base64Value, nameof(base64Value)), null, null, null);

    public static DocumentQueryValue Int32(int value) => new(StorageFieldType.Int32, null, value, null, null);

    public static DocumentQueryValue Int64(long value) => new(StorageFieldType.Int64, null, value, null, null);

    public static DocumentQueryValue Decimal(decimal value) => new(StorageFieldType.Decimal, null, value, null, null);

    public static DocumentQueryValue Boolean(bool value) => new(StorageFieldType.Boolean, null, null, value, null);

    public static DocumentQueryValue DateTimeOffset(DateTimeOffset value) => new(StorageFieldType.DateTimeOffset, null, null, null, value);

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Query values cannot be empty.", parameterName);

        return value;
    }
}

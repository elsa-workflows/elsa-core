namespace Elsa.ModularPersistence.Descriptors;

internal static class DescriptorValidation
{
    public static string RequireName(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A descriptor name cannot be empty.", parameterName);

        return value.Trim();
    }

    public static IReadOnlyList<T> RequireNonEmptyList<T>(IEnumerable<T>? values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);

        var collection = values.ToArray();
        if (collection.Length == 0)
            throw new ArgumentException("The collection must contain at least one item.", parameterName);

        return collection;
    }

    public static IReadOnlyList<string> RequireNonEmptyNames(IEnumerable<string>? values, string parameterName) =>
        RequireNonEmptyList(values?.Select(x => RequireName(x, parameterName)), parameterName);

    public static void EnsureUnique<T>(IEnumerable<T> values, Func<T, string> nameSelector, string message, string parameterName)
    {
        var duplicates = values.GroupBy(nameSelector, StringComparer.Ordinal).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        if (duplicates.Length > 0)
            throw new ArgumentException($"{message} Duplicates: {string.Join(", ", duplicates)}.", parameterName);
    }

    public static void EnsureEnumValue<TEnum>(TEnum value, string parameterName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(parameterName, value, $"Unknown {typeof(TEnum).Name} value.");
    }
}

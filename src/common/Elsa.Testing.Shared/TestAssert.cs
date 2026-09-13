using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides the small set of framework-neutral assertions used by Elsa's shared test helpers.
/// </summary>
public static class TestAssert
{
    private const int MaximumEquivalencyDepth = 50;

    /// <summary>Returns <paramref name="value"/> when it is not null; otherwise, fails the assertion.</summary>
    [return: NotNull]
    public static T NotNull<T>([NotNull] T? value, string message) where T : class
    {
        return value ?? throw new TestAssertionException(message);
    }

    /// <summary>Fails the assertion unless <paramref name="value"/> is null.</summary>
    public static void Null(object? value, string message)
    {
        if (value is not null)
            throw new TestAssertionException(message);
    }

    /// <summary>Fails the assertion unless <paramref name="condition"/> is true.</summary>
    public static void True([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
            throw new TestAssertionException(message);
    }

    /// <summary>Fails the assertion unless <paramref name="expected"/> and <paramref name="actual"/> are equal.</summary>
    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new TestAssertionException($"{message} Expected: {expected}; actual: {actual}.");
    }

    /// <summary>
    /// Fails unless every public member and collection item in <paramref name="expected"/> has an equivalent
    /// counterpart in <paramref name="actual"/>. In non-strict mode, additional actual members and collection
    /// items are allowed.
    /// </summary>
    public static void Equivalent(object? expected, object? actual, bool strict = false)
    {
        var mismatch = FindEquivalencyMismatch(
            expected,
            actual,
            strict,
            "$",
            new(ReferenceEqualityComparer.Instance),
            new(ReferenceEqualityComparer.Instance),
            depth: 1);

        if (mismatch != null)
            throw new TestAssertionException(mismatch);
    }

    private static string? FindEquivalencyMismatch(
        object? expected,
        object? actual,
        bool strict,
        string path,
        HashSet<object> expectedPath,
        HashSet<object> actualPath,
        int depth)
    {
        if (depth == MaximumEquivalencyDepth)
            return $"comparison exceeded depth {MaximumEquivalencyDepth} at {path}";

        expected = UnwrapLazy(expected);
        actual = UnwrapLazy(actual);

        if (expected == null)
            return actual == null ? null : $"expected null at {path}, but found {FormatValue(actual)}";

        if (actual == null)
            return $"expected {FormatValue(expected)} at {path}, but found null";

        if (ReferenceEquals(expected, actual))
            return null;

        if (!expectedPath.Add(expected))
            return $"expected graph contains a circular reference at {path}";

        if (!actualPath.Add(actual))
        {
            expectedPath.Remove(expected);
            return $"actual graph contains a circular reference at {path}";
        }

        try
        {
            var expectedType = expected.GetType();

            if (IsIntrinsic(expectedType))
                return IntrinsicsMatch(expected, actual) ? null : ValueMismatch(expected, actual, path);

            if (expected is DateTime or DateTimeOffset)
                return FindDateTimeMismatch(expected, actual, path);

            if (expected is FileSystemInfo expectedFileSystemInfo && actual is FileSystemInfo actualFileSystemInfo)
            {
                if (expectedType != actual.GetType())
                    return $"expected type {expectedType.FullName} at {path}, but found {actual.GetType().FullName}";

                return expectedFileSystemInfo.FullName == actualFileSystemInfo.FullName
                    ? null
                    : ValueMismatch(expectedFileSystemInfo.FullName, actualFileSystemInfo.FullName, path);
            }

            if (expected is Uri expectedUri && actual is Uri actualUri)
                return expectedUri.OriginalString == actualUri.OriginalString
                    ? null
                    : ValueMismatch(expectedUri.OriginalString, actualUri.OriginalString, path);

            if (TryGetGrouping(expected, out var expectedKey, out var expectedValues) &&
                TryGetGrouping(actual, out var actualKey, out var actualValues))
            {
                var keyMismatch = FindEquivalencyMismatch(
                    expectedKey,
                    actualKey,
                    strict: false,
                    $"{path}.Key",
                    expectedPath,
                    actualPath,
                    depth + 1);

                return keyMismatch ?? FindEnumerableMismatch(
                    expectedValues,
                    actualValues,
                    strict,
                    path,
                    expectedPath,
                    actualPath,
                    depth);
            }

            if (expected is IEnumerable expectedEnumerable && actual is IEnumerable actualEnumerable)
                return FindEnumerableMismatch(expectedEnumerable, actualEnumerable, strict, path, expectedPath, actualPath, depth);

            return FindObjectMismatch(expected, actual, strict, path, expectedPath, actualPath, depth);
        }
        finally
        {
            expectedPath.Remove(expected);
            actualPath.Remove(actual);
        }
    }

    private static string? FindDateTimeMismatch(object expected, object actual, string path)
    {
        try
        {
            if (expected is IComparable expectedComparable && expectedComparable.CompareTo(actual) == 0)
                return null;
        }
        catch (Exception exception)
        {
            return $"{ValueMismatch(expected, actual, path)} Comparison failed: {exception.Message}";
        }

        try
        {
            if (actual is IComparable actualComparable && actualComparable.CompareTo(expected) == 0)
                return null;
        }
        catch (Exception exception)
        {
            return $"{ValueMismatch(expected, actual, path)} Comparison failed: {exception.Message}";
        }

        return ValueMismatch(expected, actual, path);
    }

    private static string? FindEnumerableMismatch(
        IEnumerable expected,
        IEnumerable actual,
        bool strict,
        string path,
        HashSet<object> expectedPath,
        HashSet<object> actualPath,
        int depth)
    {
        var expectedItems = expected.Cast<object?>().ToList();
        var remainingActualItems = actual.Cast<object?>().ToList();

        foreach (var expectedItem in expectedItems)
        {
            var matchIndex = -1;

            for (var actualIndex = 0; actualIndex < remainingActualItems.Count; actualIndex++)
            {
                if (FindEquivalencyMismatch(
                        expectedItem,
                        remainingActualItems[actualIndex],
                        strict,
                        path,
                        expectedPath,
                        actualPath,
                        depth) == null)
                {
                    matchIndex = actualIndex;
                    break;
                }
            }

            if (matchIndex < 0)
                return $"no actual collection item at {path} matched expected item {FormatValue(expectedItem)}";

            remainingActualItems.RemoveAt(matchIndex);
        }

        return strict && remainingActualItems.Count > 0
            ? $"actual collection at {path} contains {remainingActualItems.Count} unexpected item(s)"
            : null;
    }

    private static string? FindObjectMismatch(
        object expected,
        object actual,
        bool strict,
        string path,
        HashSet<object> expectedPath,
        HashSet<object> actualPath,
        int depth)
    {
        var expectedMembers = GetPublicMemberGetters(expected.GetType());
        var actualMembers = GetPublicMemberGetters(actual.GetType());

        if (strict && expectedMembers.Count != actualMembers.Count)
            return $"public member counts differ at {path}: expected {expectedMembers.Count}, actual {actualMembers.Count}";

        foreach (var (name, expectedGetter) in expectedMembers)
        {
            if (!actualMembers.TryGetValue(name, out var actualGetter))
                return $"actual object is missing expected public member {path}.{name}";

            var mismatch = FindEquivalencyMismatch(
                expectedGetter(expected),
                actualGetter(actual),
                strict,
                $"{path}.{name}",
                expectedPath,
                actualPath,
                depth + 1);

            if (mismatch != null)
                return mismatch;
        }

        return null;
    }

    private static Dictionary<string, Func<object, object?>> GetPublicMemberGetters(Type type)
    {
        var fields = type
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(field => !field.IsStatic)
            .Select(field => (field.Name, Getter: new Func<object, object?>(field.GetValue)));

        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.CanRead &&
                property.GetMethod is { IsPublic: true, IsStatic: false } &&
                property.GetIndexParameters().Length == 0 &&
                !property.PropertyType.IsByRefLike &&
                !property.IsDefined(typeof(ObsoleteAttribute)) &&
                !property.GetMethod.IsDefined(typeof(ObsoleteAttribute)))
            .Select(property => (property.Name, Getter: new Func<object, object?>(property.GetValue)));

        return fields.Concat(properties).ToDictionary(item => item.Name, item => item.Getter);
    }

    private static bool TryGetGrouping(object value, out object? key, [NotNullWhen(true)] out IEnumerable? values)
    {
        var groupingInterface = value.GetType().GetInterfaces()
            .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>));

        if (groupingInterface == null || value is not IEnumerable enumerable)
        {
            key = null;
            values = null;
            return false;
        }

        key = groupingInterface.GetProperty(nameof(IGrouping<object, object>.Key))!.GetValue(value);
        values = enumerable;
        return true;
    }

    private static object? UnwrapLazy(object? value)
    {
        if (value == null)
            return null;

        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>)
            ? type.GetProperty(nameof(Lazy<object>.Value))!.GetValue(value)
            : value;
    }

    private static bool IsIntrinsic(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid);

    private static bool IntrinsicsMatch(object expected, object actual)
    {
        if (expected.Equals(actual))
            return true;

        return TryConvert(expected, actual.GetType(), out var convertedExpected) && convertedExpected.Equals(actual) ||
               TryConvert(actual, expected.GetType(), out var convertedActual) && convertedActual.Equals(expected);
    }

    private static bool TryConvert(object value, Type targetType, [NotNullWhen(true)] out object? converted)
    {
        try
        {
            converted = Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
            return converted != null;
        }
        catch (InvalidCastException)
        {
            converted = null;
            return false;
        }
    }

    private static string ValueMismatch(object? expected, object? actual, string path) =>
        $"expected {FormatValue(expected)} at {path}, but found {FormatValue(actual)}";

    private static string FormatValue(object? value) => value == null ? "null" : $"{value} ({value.GetType().Name})";
}

namespace Elsa.Workflows.Management.Options;

/// <summary>
/// Represents the options for managing host method-based activities in workflows.
/// </summary>
public class HostMethodActivitiesOptions
{
    /// <summary>
    /// Maps a registration key to a CLR type whose public async methods should be exposed as activities.
    /// </summary>
    public IDictionary<string, Type> ActivityTypes { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a new activity type to the collection of activity types.
    /// </summary>
    /// <typeparam name="T">The type of the activity to add.</typeparam>
    /// <param name="key">An optional key to associate with the activity type. If not provided, the type's name will be used.</param>
    public HostMethodActivitiesOptions AddType<T>(string? key = null) where T : class
    {
        key ??= typeof(T).Name;
        ActivityTypes[key] = typeof(T);
        return this;
    }

    /// <summary>
    /// Adds a new activity type to the collection of activity types.
    /// </summary>
    /// <param name="type">The type of the activity to add.</param>
    /// <param name="key">An optional key to associate with the activity type. If not provided, the type's name will be used.</param>
    public HostMethodActivitiesOptions AddType(Type type, string? key = null)
    {
        key ??= type.Name;
        ActivityTypes[key] = type;
        return this;
    }
}

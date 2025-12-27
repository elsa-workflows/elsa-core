namespace Elsa.Workflows.Management.Options;

/// <summary>
/// Options controlling which CLR types should be exposed as host method activities (one activity per public Task/Task&lt;T&gt; method).
/// </summary>
public class HostMethodActivitiesOptions
{
    /// <summary>
    /// Maps a registration key to a CLR type whose public async methods should be exposed as activities.
    /// </summary>
    public IDictionary<string, Type> ActivityTypes { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a type for code-first activity generation.
    /// </summary>
    public HostMethodActivitiesOptions AddType<T>(string? key = null) where T : class
    {
        key ??= typeof(T).Name;
        ActivityTypes[key] = typeof(T);
        return this;
    }

    /// <summary>
    /// Registers a type for code-first activity generation.
    /// </summary>
    public HostMethodActivitiesOptions AddType(Type type, string? key = null)
    {
        key ??= type.Name;
        ActivityTypes[key] = type;
        return this;
    }
}

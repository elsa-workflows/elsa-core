namespace Elsa.Workflows;

/// <summary>
/// Extension methods for <see cref="IActivityBuilder"/> to add HTTP activities.
/// These are placeholder methods that demonstrate the fluent API.
/// The actual HTTP activities are in the Elsa.Http module.
/// </summary>
public static class HttpActivityBuilderExtensions
{
    // Note: These methods are placeholders to demonstrate the fluent API pattern.
    // The actual implementation would require the Elsa.Http module to be referenced.
    // Users of the fluent API would add extension methods in their own code or
    // in the Elsa.Http module itself to provide these conveniences.
    
    /// <summary>
    /// Placeholder method to demonstrate HttpGet pattern.
    /// To use HTTP activities, create extension methods in the Elsa.Http module
    /// or in your own code that reference the SendHttpRequest activity.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="responseVar">Optional variable name to store the response.</param>
    /// <returns>The activity builder for chaining.</returns>
    /// <example>
    /// <code>
    /// // This is an example of how to implement this in the Elsa.Http module:
    /// public static IActivityBuilder HttpGet(this IActivityBuilder builder, string url, string? responseVar = null)
    /// {
    ///     return builder.Then&lt;SendHttpRequest&gt;(activity => 
    ///     {
    ///         activity.Url = new Input&lt;Uri?&gt;(new Uri(url));
    ///         activity.Method = new Input&lt;string&gt;("GET");
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IActivityBuilder HttpGet(this IActivityBuilder builder, string url, string? responseVar = null)
    {
        throw new NotImplementedException(
            "HTTP activities require the Elsa.Http module. " +
            "Add extension methods in the Elsa.Http module or your own code to use HTTP activities. " +
            "Example: builder.Then<SendHttpRequest>(activity => { activity.Url = new Uri(url); activity.Method = \"GET\"; })");
    }
    
    /// <summary>
    /// Placeholder method to demonstrate HttpPost pattern.
    /// To use HTTP activities, create extension methods in the Elsa.Http module
    /// or in your own code that reference the SendHttpRequest activity.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="url">The URL to send the POST request to.</param>
    /// <param name="bodyExpression">The expression for the request body.</param>
    /// <param name="responseVar">Optional variable name to store the response.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder HttpPost(this IActivityBuilder builder, string url, string? bodyExpression = null, string? responseVar = null)
    {
        throw new NotImplementedException(
            "HTTP activities require the Elsa.Http module. " +
            "Add extension methods in the Elsa.Http module or your own code to use HTTP activities. " +
            "Example: builder.Then<SendHttpRequest>(activity => { activity.Url = new Uri(url); activity.Method = \"POST\"; activity.Content = ...; })");
    }
}

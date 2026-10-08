namespace Elsa.Studio.Workflows.UI.Models;

/// <summary>
/// A static class with default activity colors.
/// </summary>
public static class DefaultActivityColors
{
    /// <summary>
    /// Gets or sets the default.
    /// </summary>
    public static string Default { get; set; } = "var(--mud-palette-primary)";
    /// <summary>
    /// Gets or sets the not found.
    /// </summary>
    public static string NotFound { get; set; } = "var(--mud-palette-error)";
    /// <summary>
    /// Gets or sets the composition.
    /// </summary>
    public static string Composition { get; set; } = "#f97316";
    /// <summary>
    /// Gets or sets the console.
    /// </summary>
    public static string Console { get; set; } = "#0369a1";
    /// <summary>
    /// Gets or sets the http.
    /// </summary>
    public static string Http { get; set; } = "#2dd4bf";
    /// <summary>
    /// Gets or sets the scripting.
    /// </summary>
    public static string Scripting { get; set; } = "#22c55e";
    /// <summary>
    /// Gets or sets the flowchart.
    /// </summary>
    public static string Flowchart { get; set; } = "#06b6d4";
    /// <summary>
    /// Gets or sets the looping.
    /// </summary>
    public static string Looping { get; set; } = "#6366f1";
    /// <summary>
    /// Gets or sets the primitives.
    /// </summary>
    public static string Primitives { get; set; } = Default;
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public static string Email { get; set; } = "#fcd34d";
    /// <summary>
    /// Gets or sets the branching.
    /// </summary>
    public static string Branching { get; set; } = "#06b6d4";
    /// <summary>
    /// Gets or sets the timer.
    /// </summary>
    public static string Timer { get; set; } = "#d946ef";
    /// <summary>
    /// Gets or sets the diagnostics.
    /// </summary>
    public static string Diagnostics { get; set; } = "#ec407a";
    /// <summary>
    /// Gets or sets the BPMN colour, used by an <c>Elsa.BpmnProcess</c> scope wherever it is composed.
    /// </summary>
    public static string Bpmn { get; set; } = "#7c3aed";
}
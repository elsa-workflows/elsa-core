using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Core.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// This activity is instantiated in case a workflow references an activity type that could not be found.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "System", "A placeholder activity that will be used in case a workflow definition references an activity type that cannot be found.")]
[PublicAPI]
public class NotFoundActivity : CodeActivity
{
    /// <inheritdoc />
    public NotFoundActivity([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public NotFoundActivity(string missingTypeName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        MissingTypeName = missingTypeName;
    }
    
    /// <summary>
    /// The type name of the missing activity type.
    /// </summary>
    public string MissingTypeName { get; set; } = default!;
    
    /// <summary>
    /// The version of the missing activity type.
    /// </summary>
    public int MissingTypeVersion { get; set; }

    /// <summary>
    /// The original activity JSON.
    /// </summary>
    public string OriginalActivityJson { get; set; } = default!;
}
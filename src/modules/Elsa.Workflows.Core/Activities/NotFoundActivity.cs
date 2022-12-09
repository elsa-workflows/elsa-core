using System.ComponentModel;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// This activity is instantiated in case a workflow references an activity type that could not be found.
/// </summary>
[Browsable(false)]
public class NotFoundActivity : Activity
{
    /// <inheritdoc />
    public NotFoundActivity()
    {
    }

    /// <inheritdoc />
    public NotFoundActivity(string missingTypeName)
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
}
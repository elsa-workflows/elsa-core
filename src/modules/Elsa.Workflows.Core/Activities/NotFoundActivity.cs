using System.ComponentModel;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

[Browsable(false)]
public class NotFoundActivity : Activity
{
    public NotFoundActivity()
    {
    }

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
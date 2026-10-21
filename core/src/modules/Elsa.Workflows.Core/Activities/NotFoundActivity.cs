using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.Exceptions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

/// <summary>
/// This activity is instantiated in case a workflow references an activity type that could not be found.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "System", "A placeholder activity that will be used in case a workflow definition references an activity type that cannot be found.")]
[PublicAPI]
public class NotFoundActivity : CodeActivity
{
    private string _missingTypeName = null!;
    private string? _originalActivityJson;

    /// <inheritdoc />
    public NotFoundActivity([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public NotFoundActivity(string missingTypeName, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
        MissingTypeName = missingTypeName;
    }
    
    /// <summary>
    /// The type name of the missing activity type.
    /// </summary>
    public string MissingTypeName
    {
        get => _missingTypeName;
        set
        {
            var previousType = _missingTypeName;
            _missingTypeName = value;
            if (!string.Equals(previousType, value, StringComparison.Ordinal))
                CompensationScopeExecutionState.OnMissingActivityTypeChanged(previousType, value);
        }
    }
    
    /// <summary>
    /// The version of the missing activity type.
    /// </summary>
    public int MissingTypeVersion { get; set; }

    /// <summary>
    /// The original activity JSON.
    /// </summary>
    public string OriginalActivityJson
    {
        get => _originalActivityJson!;
        set
        {
            var previousJson = _originalActivityJson;
            _originalActivityJson = value;
            if (!string.Equals(previousJson, value, StringComparison.Ordinal))
                CompensationScopeExecutionState.OnOriginalActivityJsonChanged();
        }
    }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        throw new ActivityNotFoundException(MissingTypeName, MissingTypeVersion);
    }
}

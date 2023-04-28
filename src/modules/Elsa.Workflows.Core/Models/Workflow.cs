using System.ComponentModel;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents an executable process.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Workflows", "A workflow is an activity that executes its Root activity.")]
[PublicAPI]
public class Workflow : Composite, ICloneable
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public Workflow(
        WorkflowIdentity identity,
        WorkflowPublication publication,
        WorkflowMetadata workflowMetadata,
        WorkflowOptions? options,
        IActivity root,
        ICollection<Variable> variables,
        IDictionary<string, object> customProperties)
    {
        Identity = identity;
        Publication = publication;
        WorkflowMetadata = workflowMetadata;
        Options = options;
        Variables = variables;
        CustomProperties = customProperties;
        Root = root;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public Workflow(IActivity root)
    {
        Root = root;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public Workflow()
    {
    }

    public WorkflowIdentity Identity { get; set; } = WorkflowIdentity.VersionOne;
    public WorkflowPublication Publication { get; set; } = WorkflowPublication.LatestAndPublished;
    public WorkflowMetadata WorkflowMetadata { get; set; } = new();
    public WorkflowOptions? Options { get; set; }
    
    /// <summary>
    /// Constructs a new <see cref="Workflow"/> from the specified <see cref="IActivity"/>.
    /// </summary>
    public static Workflow FromActivity(IActivity root) => root as Workflow ?? new(root);

    /// <summary>
    /// Creates a new memory register initialized with this workflow's variables.
    /// </summary>
    public MemoryRegister CreateRegister()
    {
        var register = new MemoryRegister();
        register.Declare(Variables);
        return register;
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if(ResultVariable != null)
            context.WorkflowExecutionContext.MemoryRegister.Declare(ResultVariable);

        await base.ExecuteAsync(context);
    }

    /// <summary>
    /// Create a shallow copy of this workflow.
    /// </summary>
    public Workflow Clone() => (Workflow)((ICloneable)this).Clone();
    
    object ICloneable.Clone() => MemberwiseClone();
}
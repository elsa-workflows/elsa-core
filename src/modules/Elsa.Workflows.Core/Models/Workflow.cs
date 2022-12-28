using System.ComponentModel;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents an executable process.
/// </summary>
[Browsable(false)]
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
        Version = identity.Version;
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

    public WorkflowIdentity Identity { get; set; } = WorkflowIdentity.VersionOne;
    public WorkflowPublication Publication { get; set; } = WorkflowPublication.LatestAndPublished;
    public WorkflowMetadata WorkflowMetadata { get; set; } = new();
    public WorkflowOptions? Options { get; set; }

    /// <summary>
    /// Create a shallow copy of this workflow.
    /// </summary>
    public Workflow Clone() => (Workflow)((ICloneable)this).Clone();
    
    object ICloneable.Clone() => MemberwiseClone();
}
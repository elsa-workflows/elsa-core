using System.ComponentModel;
using Elsa.Expressions.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Represents an executable process.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Workflows", "A workflow is an activity that executes its Root activity.")]
[PublicAPI]
public class Workflow : Composite<object>, ICloneable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Workflow"/> class.
    /// </summary>
    public Workflow(
        WorkflowIdentity identity,
        WorkflowPublication publication,
        WorkflowMetadata workflowMetadata,
        WorkflowOptions options,
        IActivity root,
        ICollection<Variable> variables,
        ICollection<InputDefinition> inputs,
        ICollection<OutputDefinition> outputs,
        ICollection<string> outcomes,
        IDictionary<string, object> customProperties,
        bool isReadonly,
        bool isSystem)
    {
        Identity = identity;
        Publication = publication;
        Inputs = inputs;
        Outputs = outputs;
        Outcomes = outcomes;
        WorkflowMetadata = workflowMetadata;
        Options = options;
        Variables = variables;
        CustomProperties = customProperties;
        Root = root;
        IsReadonly = isReadonly;
        IsSystem = isSystem;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Workflow"/> class.
    /// </summary>
    public Workflow(IActivity root) : this()
    {
        Root = root;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Workflow"/> class.
    /// </summary>
    public Workflow()
    {
    }

    /// <summary>
    /// Gets or sets the workflow identity.
    /// </summary>
    public WorkflowIdentity Identity { get; set; } = WorkflowIdentity.VersionOne;
    
    /// <summary>
    /// Gets or sets the publication status of the workflow.
    /// </summary>
    public WorkflowPublication Publication { get; set; } = WorkflowPublication.LatestAndPublished;
    
    /// <summary>
    /// Gets or sets input definitions.
    /// </summary>
    public ICollection<InputDefinition> Inputs { get; set; } = new List<InputDefinition>();
    
    /// <summary>
    /// Gets or sets output definitions.
    /// </summary>
    public ICollection<OutputDefinition> Outputs { get; set; } = new List<OutputDefinition>();
    
    /// <summary>
    /// Gets or sets possible outcomes for this workflow.
    /// </summary>
    public ICollection<string> Outcomes { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets metadata about the workflow.
    /// </summary>
    public WorkflowMetadata WorkflowMetadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets options for the workflow.
    /// </summary>
    public WorkflowOptions Options { get; set; } = new();
    
    /// <summary>
    /// Make workflow definition readonly.
    /// </summary>
    public bool IsReadonly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the workflow is a system workflow.
    /// </summary>
    public bool IsSystem { get; }
    
    /// <summary>
    /// Returns the workflow definition handle.
    /// </summary>
    public WorkflowDefinitionHandle DefinitionHandle => WorkflowDefinitionHandle.ByDefinitionVersionId(Identity.Id);

    /// <summary>
    /// Constructs a new <see cref="Workflow"/> from the specified <see cref="IActivity"/>.
    /// </summary>
    public static Workflow FromActivity(IActivity root) => root as Workflow ?? new(root);

    /// <summary>
    /// Creates a new memory register initialized with this workflow's variables.
    /// </summary>
    public MemoryRegister CreateRegister()
    {
        return new MemoryRegister();
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
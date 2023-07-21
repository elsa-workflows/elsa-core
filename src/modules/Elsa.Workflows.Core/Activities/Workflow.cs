using System.ComponentModel;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Represents an executable process.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Workflows", "A workflow is an activity that executes its Root activity.")]
[PublicAPI]
public class Workflow : Composite<object>, ICloneable
{
    /// <summary>
    /// Constructor.
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
        bool isReadonly)
    {
        Identity = identity;
        Publication = publication;
        Inputs = inputs;
        Outputs = outputs;
        Outcomes = outcomes;
        WorkflowMetadata = workflowMetadata;
        ToolVersion = new Version(1, 0);
        Options = options;
        Variables = variables;
        CustomProperties = customProperties;
        Root = root;
        IsReadonly = isReadonly;
    }

    /// <summary>
    /// Constructor.
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
        ToolVersion = new Version(1, 0);
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
    /// The version of the tool that created this workflow.
    /// </summary>
    public Version ToolVersion { get; set; }

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
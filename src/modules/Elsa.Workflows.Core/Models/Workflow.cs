using System.ComponentModel;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

[Browsable(false)]
public class Workflow : Composite, ICloneable
{
    public Workflow(
        WorkflowIdentity identity,
        WorkflowPublication publication,
        WorkflowMetadata workflowMetadata,
        IActivity root,
        ICollection<Variable> variables,
        IDictionary<string, object> metadata,
        IDictionary<string, object> applicationProperties)
    {
        Identity = identity;
        Publication = publication;
        WorkflowMetadata = workflowMetadata;
        Variables = variables;
        Metadata = metadata;
        ApplicationProperties = applicationProperties;
        Root = root;
        Version = identity.Version;
    }

    public Workflow(IActivity root)
    {
        Root = root;
    }

    public Workflow()
    {
    }

    public static Workflow FromActivity(IActivity root) => root is Workflow workflow ? workflow : new(root);

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
    public ICollection<Variable> Variables { get; init; } = new List<Variable>();
    public Workflow Clone() => (Workflow)((ICloneable)this).Clone();
    object ICloneable.Clone() => MemberwiseClone();
}
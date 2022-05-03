using Elsa.Activities;
using Elsa.Services;

namespace Elsa.Models;

public class Workflow : Composite, ICloneable
{
    public Workflow(
        WorkflowIdentity identity,
        WorkflowPublication publication,
        WorkflowMetadata workflowMetadata,
        IActivity root,
        ICollection<Variable> variables,
        ICollection<string> tags,
        IDictionary<string, object> metadata,
        IDictionary<string, object> applicationProperties)
    {
        Identity = identity;
        Publication = publication;
        WorkflowMetadata = workflowMetadata;
        Variables = variables;
        Tags = tags;
        Metadata = metadata;
        ApplicationProperties = applicationProperties;
        Root = root;
    }

    public static Workflow FromActivity(IActivity root) => new(
        WorkflowIdentity.VersionOne,
        WorkflowPublication.LatestDraft,
        new WorkflowMetadata(),
        root,
        new List<Variable>(),
        new List<string>(),
        new Dictionary<string, object>(),
        new Dictionary<string, object>());

    /// <summary>
    /// Creates a new memory register initialized with this workflow's variables.
    /// </summary>
    public Register CreateRegister()
    {
        var register = new Register();
        register.Declare(Variables);
        return register;
    }

    public WorkflowIdentity Identity { get; set; }
    public WorkflowPublication Publication { get; set; }
    public WorkflowMetadata WorkflowMetadata { get; set; }
    public ICollection<Variable> Variables { get; init; }
    public ICollection<string> Tags { get; init; }
    public Workflow Clone() => (Workflow)((ICloneable)this).Clone();
    object ICloneable.Clone() => MemberwiseClone();
}
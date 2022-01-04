using Elsa.Contracts;

namespace Elsa.Models;

public record Workflow(
    WorkflowIdentity Identity,
    WorkflowPublication Publication,
    WorkflowMetadata Metadata,
    IActivity Root,
    ICollection<ITrigger> Triggers)
{
    public static Workflow FromActivity(IActivity root) => new(WorkflowIdentity.VersionOne, WorkflowPublication.LatestDraft, new WorkflowMetadata(), root, new List<ITrigger>());

    public Workflow WithVersion(int version) => this with { Identity = Identity with { Version = version } };
    public Workflow IncrementVersion() => WithVersion(Identity.Version + 1);
    public Workflow WithPublished(bool value = true) => this with { Publication = Publication with { IsPublished = value } };
    public Workflow WithLatest(bool value = true) => this with { Publication = Publication with { IsLatest = value } };
    public Workflow WithId(string value) => this with { Identity = Identity with { Id = value } };
    public Workflow WithDefinitionId(string value) => this with { Identity = Identity with { DefinitionId = value } };
    
}
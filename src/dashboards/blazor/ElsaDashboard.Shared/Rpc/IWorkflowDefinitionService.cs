using ProtoBuf;

namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public interface IWorkflowDefinitionService
    {
        //ValueTask<ICollection<Test>> GetAsync(string workflowDefinitionId, VersionOptions version);
    }

    public record Test(int a, bool b);
}
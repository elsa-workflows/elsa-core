using ProtoBuf;

namespace ElsaDashboard.Blazor.Application.Rpc
{
    [ProtoContract]
    public interface IWorkflowDefinitionService
    {
        //ValueTask<ICollection<Test>> GetAsync(string workflowDefinitionId, VersionOptions version);
    }

    public record Test(int a, bool b);
}
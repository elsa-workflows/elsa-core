using System.Collections.Generic;
using System.Threading.Tasks;
using ProtoBuf;

namespace ElsaDashboard.Blazor.Shared.Rpc
{
    [ProtoContract]
    public interface IWorkflowDefinitionService
    {
        ValueTask<ICollection<Test>> GetAsync(string workflowDefinitionId, VersionOptions version);
    }

    public record Test(int a, bool b);
}
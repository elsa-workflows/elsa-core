using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace Elsa.Client.Services
{
    public interface IScriptingApi
    {
        [Get("/v1/scripting/javascript/type-definitions/{workflowDefinitionId}")]
        Task<HttpContent> GetTypeScriptDefinitionFileAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    }
}
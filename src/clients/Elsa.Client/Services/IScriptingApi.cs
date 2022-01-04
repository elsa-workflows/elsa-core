using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IScriptingApi
    {
        [Post("/v1/scripting/javascript/type-definitions/{workflowDefinitionId}")]
        Task<HttpContent> GetTypeScriptDefinitionFileAsync(string workflowDefinitionId, [Body] GetTypeScriptDefinitionFileRequest? context, CancellationToken cancellationToken = default);
    }
}
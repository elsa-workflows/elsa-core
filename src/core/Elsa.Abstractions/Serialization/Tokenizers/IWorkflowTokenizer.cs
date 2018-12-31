using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Tokenizers
{
    public interface IWorkflowTokenizer
    {
        Task<JToken> TokenizeWorkflowAsync(Workflow value, CancellationToken cancellationToken);
        Task<Workflow> DetokenizeWorkflowAsync(JToken token, CancellationToken cancellationToken);
        Task<JToken> TokenizeActivityAsync(IActivity value, CancellationToken cancellationToken);
        Task<IActivity> DetokenizeActivityAsync(JToken token, CancellationToken cancellationToken);
    }
}
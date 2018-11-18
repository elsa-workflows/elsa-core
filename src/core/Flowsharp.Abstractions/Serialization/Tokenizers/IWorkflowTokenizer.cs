using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization.Tokenizers
{
    public interface IWorkflowTokenizer
    {
        Task<JToken> TokenizeAsync(Workflow value, CancellationToken cancellationToken);
        Task<Workflow> DetokenizeAsync(JToken token, CancellationToken cancellationToken);
    }
}
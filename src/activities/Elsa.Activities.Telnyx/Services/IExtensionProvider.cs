using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;

namespace Elsa.Activities.Telnyx.Services
{
    public interface IExtensionProvider
    {
        Task<Extension?> GetAsync(string extensionNumber, CancellationToken cancellationToken = default);
    }
}
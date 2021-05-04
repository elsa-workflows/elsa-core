using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;

namespace Elsa.Activities.Telnyx.Services
{
    public class NullExtensionProvider : IExtensionProvider
    {
        public Task<Extension?> GetAsync(string extensionNumber, CancellationToken cancellationToken = default) => Task.FromResult<Extension?>(default);
    }
}
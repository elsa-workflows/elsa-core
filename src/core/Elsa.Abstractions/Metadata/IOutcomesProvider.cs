using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Metadata
{
    public interface IOutcomesProvider
    {
        ValueTask<IEnumerable<string>> GetOutcomesAsync(CancellationToken cancellationToken);
    }
}
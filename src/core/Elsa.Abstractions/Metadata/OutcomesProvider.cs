using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Metadata
{
    public abstract class OutcomesProvider : IOutcomesProvider
    {
        public virtual ValueTask<IEnumerable<string>> GetOutcomesAsync(CancellationToken cancellationToken) => new(GetOutcomes());
        protected virtual IEnumerable<string> GetOutcomes() => Enumerable.Empty<string>();
    }
}
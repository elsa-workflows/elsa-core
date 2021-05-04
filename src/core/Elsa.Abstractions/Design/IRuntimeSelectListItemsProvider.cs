using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Design
{
    public interface IRuntimeSelectListItemsProvider
    {
        ValueTask<IEnumerable<SelectListItem>> GetItemsAsync(object? context = default, CancellationToken cancellationToken = default);
    }
}
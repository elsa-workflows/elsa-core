using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Design
{
    public interface IRuntimeSelectListProvider
    {
        ValueTask<SelectList> GetSelectListAsync(object? context = default, CancellationToken cancellationToken = default);
    }
}
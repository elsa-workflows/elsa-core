using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public interface IActivityDesignerStore
    {
        Task<IEnumerable<ActivityDesignerDescriptor>> ListAsync(CancellationToken cancellationToken);
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public interface IActivityDesignerProvider
    {
        Task<IEnumerable<ActivityDesignerDescriptor>> DescribeActivityDesignersAsync(CancellationToken cancellationToken);
    }
}
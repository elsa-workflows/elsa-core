using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Web.Shapes;
using OrchardCore.DisplayManagement;

namespace Elsa.Web.Services
{
    public interface IActivityShapeFactory
    {
        Task<ActivityWrapper> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken);
        Task<ActivityCard> BuildCardShapeAsync(IActivity activity, CancellationToken cancellationToken);
    }
}
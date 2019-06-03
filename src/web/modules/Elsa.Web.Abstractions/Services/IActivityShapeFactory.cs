using System.Threading;
using System.Threading.Tasks;
using Elsa.Web.Shapes;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Services
{
    public interface IActivityShapeFactory
    {
        Task<ActivityWrapper> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken);
        Task<ActivityCard> BuildCardShapeAsync(IActivity activity, CancellationToken cancellationToken);
        Task<ActivityEditor> BuildEditShapeAsync(IActivity activity, CancellationToken cancellationToken);
    }
}
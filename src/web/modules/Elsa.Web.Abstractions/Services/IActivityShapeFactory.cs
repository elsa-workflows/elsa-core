using System.Threading;
using System.Threading.Tasks;
using OrchardCore.DisplayManagement;

namespace Elsa.Web.Services
{
    public interface IActivityShapeFactory
    {
        Task<IShape> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken);
        Task<IShape> BuildCardShapeAsync(IActivity activity, CancellationToken cancellationToken);
    }
}
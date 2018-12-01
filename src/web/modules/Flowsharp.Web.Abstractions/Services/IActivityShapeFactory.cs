using System.Threading;
using System.Threading.Tasks;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.Abstractions.Services
{
    public interface IActivityShapeFactory
    {
        Task<IShape> BuildDesignShapeAsync(IActivity activity, CancellationToken cancellationToken);
    }
}
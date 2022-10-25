using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IIdentityGraphService
{
    Task AssignIdentitiesAsync(Workflow workflow, CancellationToken cancellationToken = default);
    Task AssignIdentitiesAsync(IActivity root, CancellationToken cancellationToken = default);
    void AssignIdentities(ActivityNode root);
}
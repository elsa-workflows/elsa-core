using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IIdentityGraphService
{
    void AssignIdentities(Workflow workflow);
    void AssignIdentities(IActivity root);
    void AssignIdentities(ActivityNode root);
}
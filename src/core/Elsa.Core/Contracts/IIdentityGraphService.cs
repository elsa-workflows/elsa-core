using Elsa.Models;

namespace Elsa.Contracts;

public interface IIdentityGraphService
{
    void AssignIdentities(Workflow workflow);
    void AssignIdentities(IActivity root);
    void AssignIdentities(ActivityNode root);
}
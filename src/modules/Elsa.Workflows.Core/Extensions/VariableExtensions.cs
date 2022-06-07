using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public static class VariableExtensions
{
    public static Variable WithWorkflowDrive(this Variable variable) => variable.WithStorage(StorageDriverNames.Workflow);
    public static Variable WithMemoryDrive(this Variable variable) => variable.WithStorage(StorageDriverNames.Memory);
    
    public static Variable WithStorage(this Variable variable, string storageDriverId)
    {
        variable.StorageDriverId = storageDriverId;
        return variable;
    }
}
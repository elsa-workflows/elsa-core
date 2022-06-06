using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public static class VariableExtensions
{
    public static Variable WithWorkflowDrive(this Variable variable) => variable.WithDrive(DataDriveNames.Workflow);
    public static Variable WithMemoryDrive(this Variable variable) => variable.WithDrive(DataDriveNames.Memory);
    
    public static Variable WithDrive(this Variable variable, string driveId)
    {
        variable.DriveId = driveId;
        return variable;
    }
}
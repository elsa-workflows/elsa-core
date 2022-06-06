namespace Elsa.Workflows.Core.Services;

public interface IDataDriveManager
{
    IDataDrive? GetDriveById(string id);
}
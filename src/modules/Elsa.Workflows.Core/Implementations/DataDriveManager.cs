using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class DataDriveManager : IDataDriveManager
{
    private readonly IEnumerable<IDataDrive> _drives;
    public DataDriveManager(IEnumerable<IDataDrive> drives) => _drives = drives;
    public IDataDrive? GetDriveById(string id) => _drives.FirstOrDefault(x => x.Id == id);
}
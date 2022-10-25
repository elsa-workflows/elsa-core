using System.Collections.Generic;
using System.Linq;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class StorageDriverManager : IStorageDriverManager
{
    private readonly IEnumerable<IStorageDriver> _drivers;
    public StorageDriverManager(IEnumerable<IStorageDriver> drivers) => _drivers = drivers;
    public IStorageDriver? GetDriveById(string id) => _drivers.FirstOrDefault(x => x.Id == id);
    public IEnumerable<IStorageDriver> List() => _drivers;
}
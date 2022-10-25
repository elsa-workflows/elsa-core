using System.Collections.Generic;

namespace Elsa.Workflows.Core.Services;

public interface IStorageDriverManager
{
    IStorageDriver? GetDriveById(string id);
    IEnumerable<IStorageDriver> List();
}
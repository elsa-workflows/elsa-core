using Elsa.DataSets.Models;

namespace Elsa.DataSets.Contracts;

public interface IDataSet
{
    LinkedServiceReference LinkedServiceReference { get; set; }
}
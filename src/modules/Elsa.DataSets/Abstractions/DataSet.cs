using Elsa.DataSets.Contracts;
using Elsa.DataSets.Models;

namespace Elsa.DataSets.Abstractions;

public abstract class DataSet : IDataSet
{
    public LinkedServiceReference LinkedServiceReference { get; set; }
}
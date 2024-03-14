namespace Elsa.DataSets.Models;

public interface IDataSource
{
    ValueTask<IEnumerable<object>> ListAsync(DataSourceContext context);
}
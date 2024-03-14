namespace Elsa.DataSets.Models;

public interface IDataSource
{
    IAsyncEnumerable<T> ListAsync<T>(DataSourceContext context);
}
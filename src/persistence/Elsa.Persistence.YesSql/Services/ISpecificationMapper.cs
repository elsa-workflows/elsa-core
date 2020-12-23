using Elsa.Persistence.Specifications;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Services
{
    public interface ISpecificationMapper<T, TDocument, TIndex>
        where TDocument : class
        where TIndex : IIndex
    {
        IQuery<TDocument, TIndex> Map(ISpecification<T> specification);
    }
}
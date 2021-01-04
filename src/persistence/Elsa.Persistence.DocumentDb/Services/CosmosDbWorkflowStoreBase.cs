using System.Collections.Generic;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Services;

namespace Elsa.Persistence.DocumentDb.Services
{
    public abstract class CosmosDbWorkflowStoreBase<TModel, TDocument> where TDocument : DocumentBase
    {
        private readonly IMapper mapper;
        protected readonly ICosmosDbStoreHelper<TDocument> cosmosDbStoreHelper;

        protected CosmosDbWorkflowStoreBase(IMapper mapper, ICosmosDbStoreHelper<TDocument> cosmosDbStoreHelper) =>
            (this.mapper, this.cosmosDbStoreHelper) = (mapper, cosmosDbStoreHelper);

        protected TDocument Map(TModel source) => mapper.Map<TDocument>(source);
        protected TModel Map(TDocument source) => mapper.Map<TModel>(source);
        protected IEnumerable<TModel> Map(IEnumerable<TDocument> source) => mapper.Map<IEnumerable<TModel>>(source);
    }
}
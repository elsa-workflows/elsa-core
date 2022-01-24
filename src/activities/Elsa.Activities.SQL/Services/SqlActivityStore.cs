using AutoMapper;
using Elsa.Activities.SQL.Persistence;
using Elsa.Persistence;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Elsa.Webhooks.Persistence.EntityFramework.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.SQL.Services
{
    public class SqlActivityStore : EntityFrameworkStore<SqlActivity, SqlActivityContext>, ISqlActivityStore
    {
        private readonly IContentSerializer _contentSerializer;

        public SqlActivityStore(ISqlActivityFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer) : base(dbContextFactory, mapper)
        {
            _contentSerializer = contentSerializer;
            
        }

        protected override Expression<Func<SqlActivity, bool>> MapSpecification(ISpecification<SqlActivity> specification) => AutoMapSpecification(specification);
        public IContextFactory<SqlActivityContext> GetContext() => (IContextFactory<SqlActivityContext>)DbContextFactory.CreateDbContext();
    }

    public interface ISqlActivityStore : IStore<SqlActivity>
    {
    }

 
}

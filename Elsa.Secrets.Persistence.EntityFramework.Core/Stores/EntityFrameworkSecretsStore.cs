using AutoMapper;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EntityFramework.Core.Services;
using Elsa.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkSecretsStore : EntityFrameworkStore<Secret, SecretsContext>, ISecretsStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkSecretsStore(ISecretsContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer) : base(dbContextFactory, mapper)
        {
            _contentSerializer = contentSerializer;
        }

        protected override Expression<Func<Secret, bool>> MapSpecification(ISpecification<Secret> specification) => AutoMapSpecification(specification);
    }
}

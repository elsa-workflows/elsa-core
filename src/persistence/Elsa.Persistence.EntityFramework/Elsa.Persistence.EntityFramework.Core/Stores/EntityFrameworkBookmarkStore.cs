using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkBookmarkStore : ElsaContextEntityFrameworkStore<Bookmark>, IBookmarkStore
    {
        public EntityFrameworkBookmarkStore(Func<IElsaContextFactory> getDbContextFactory, IMapper mapper) : base(getDbContextFactory, mapper)
        {
        }

        protected override Expression<Func<Bookmark, bool>> MapSpecification(ISpecification<Bookmark> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}
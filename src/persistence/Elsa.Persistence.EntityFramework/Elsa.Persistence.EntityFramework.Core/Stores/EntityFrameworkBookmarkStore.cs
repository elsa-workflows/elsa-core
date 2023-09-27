using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;
using Microsoft.Extensions.Logging;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkBookmarkStore : ElsaContextEntityFrameworkStore<Bookmark>, IBookmarkStore
    {
        public EntityFrameworkBookmarkStore(IElsaContextFactory dbContextFactory, IMapper mapper, ILogger<EntityFrameworkBookmarkStore> logger) : base(dbContextFactory, mapper, logger)
        {
        }

        protected override Expression<Func<Bookmark, bool>> MapSpecification(ISpecification<Bookmark> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}
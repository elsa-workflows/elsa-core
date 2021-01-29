using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkBookmarkStore : EntityFrameworkStore<Bookmark>, IBookmarkStore
    {
        public EntityFrameworkBookmarkStore(ElsaContext dbContext) : base(dbContext)
        {
        }

        protected override DbSet<Bookmark> DbSet => DbContext.Bookmarks;
        
        protected override Expression<Func<Bookmark, bool>> MapSpecification(ISpecification<Bookmark> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}
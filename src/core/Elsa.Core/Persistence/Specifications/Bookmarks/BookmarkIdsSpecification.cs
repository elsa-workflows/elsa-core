using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Bookmarks
{
    public class BookmarkIdsSpecification : Specification<Bookmark>
    {
        public BookmarkIdsSpecification(IEnumerable<string> ids) => Ids = ids;
        public IEnumerable<string> Ids { get; }
        public override Expression<Func<Bookmark, bool>> ToExpression() => bookmark => Ids.Contains(bookmark.Id);
    }
}
using System;
using System.Linq.Expressions;

namespace Elsa.Persistence.Specifications
{
    public interface IOrderBy<T>
    {
        Expression<Func<T, object>> OrderByExpression { get; }
        SortDirection SortDirection { get; } 
    }

    public class OrderBy<T> : IOrderBy<T>
    {
        public OrderBy(Expression<Func<T, object>> orderByExpression, SortDirection sortDirection)
        {
            OrderByExpression = orderByExpression;
            SortDirection = sortDirection;
        }
        
        public Expression<Func<T, object>> OrderByExpression { get; }
        public SortDirection SortDirection { get; }
    }

    public static class OrderBySpecification
    {
        public static OrderBy<T> OrderBy<T>(Expression<Func<T, object>> expression, SortDirection sortDirection = SortDirection.Ascending) => new(expression, sortDirection);
        public static OrderBy<T> OrderByDescending<T>(Expression<Func<T, object>> expression, SortDirection sortDirection = SortDirection.Descending) => new(expression, sortDirection);
    }
}
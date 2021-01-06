using System;
using System.Collections.Generic;

using Elsa.Indexing.Models;

using Nest;

namespace Elsa.Indexing.Extensions
{
    public static class ElasticsearchExtensions
    {
        public static void AddWhenNotEmpty<TEntity>(this List<Func<QueryContainerDescriptor<TEntity>, 
            QueryContainer>> query, Func<QueryContainerDescriptor<TEntity>, QueryContainer> func, string? value) where TEntity : class,  IElasticEntity
        {
            if(string.IsNullOrEmpty(value) == false)
            {
                query.Add(func);
            }
        }

        public static void AddWhenNotEmpty<TEntity, TValue>(this List<Func<QueryContainerDescriptor<TEntity>,
            QueryContainer>> query, Func<QueryContainerDescriptor<TEntity>, QueryContainer> func, Nullable<TValue> value) 
            where TEntity : class, IElasticEntity
            where TValue : struct
        {
            if (value.HasValue)
            {
                query.Add(func);
            }
        }
    }
}

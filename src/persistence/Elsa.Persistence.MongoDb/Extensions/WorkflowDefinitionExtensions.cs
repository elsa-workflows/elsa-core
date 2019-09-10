using Elsa.Models;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Extensions
{
    public static class WorkflowDefinitionExtensions
    {
//        public static IMongoQueryable<WorkflowDefinition> WithVersion(this IMongoQueryable<WorkflowDefinition> query,
//            VersionOptions version)
//        {
//            if (version.IsDraft)
//                query = query.Where(x => !x.IsPublished).OrderByDescending(x => x.Version);
//            if (version.IsLatest)
//                query = query.Where(x => x.IsLatest);
//            if (version.IsPublished)
//                query = query.Where(x => x.IsPublished).OrderByDescending(x => x.Version);
//            if (version.Version > 0)
//                query = query.Where(x => x.Version == version.Version);
//
//            return query;
//        }
    }
}
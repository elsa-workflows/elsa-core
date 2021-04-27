using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class VersionOptionsSpecification : Specification<WorkflowDefinition>
    {
        public VersionOptions VersionOptions { get; set; }
        public VersionOptionsSpecification(VersionOptions versionOptions) => VersionOptions = versionOptions;
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression()
        {
            if (VersionOptions.AllVersions)
                return x => true;
            if (VersionOptions.IsDraft)
                return x => !x.IsPublished;
            if (VersionOptions.IsLatest)
                return x => x.IsLatest;
            if (VersionOptions.IsPublished)
                return x => x.IsPublished;
            if (VersionOptions.IsLatestOrPublished)
                return x => x.IsPublished || x.IsLatest;
            if (VersionOptions.Version > 0)
                return x => x.Version == VersionOptions.Version;

            return x => false;
        }
    }
}
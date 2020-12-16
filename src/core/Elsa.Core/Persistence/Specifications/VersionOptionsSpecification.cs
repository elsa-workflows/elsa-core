using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class VersionOptionsSpecification : Specification<WorkflowDefinition>
    {
        public VersionOptions VersionOptions { get; set; }
        public VersionOptionsSpecification(VersionOptions versionOptions) => VersionOptions = versionOptions;
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => x => x.WithVersion(VersionOptions);
    }
}
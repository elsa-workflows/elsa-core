using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public static class WorkflowDefinitionSpecificationExtensions
    {
        public static ISpecification<WorkflowDefinition> WithVersionOptions(this ISpecification<WorkflowDefinition> specification, VersionOptions versionOptions) => specification.And(new VersionOptionsSpecification(versionOptions));
    }
}
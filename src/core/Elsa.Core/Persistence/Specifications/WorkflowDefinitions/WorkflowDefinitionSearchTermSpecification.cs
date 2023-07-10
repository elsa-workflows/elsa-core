using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions;

public class WorkflowDefinitionSearchTermSpecification : Specification<WorkflowDefinition>
{
    public WorkflowDefinitionSearchTermSpecification(string searchTerm)
    {
        SearchTerm = searchTerm;
    }

    public string SearchTerm { get; set; }

    public override Expression<Func<WorkflowDefinition, bool>> ToExpression()=> x => 
        x.Name!.Contains(SearchTerm) ||
        x.DisplayName!.Contains(SearchTerm) ||
        x.Description!.Contains(SearchTerm) ||
        x.Tag!.Contains(SearchTerm);
}
using System.Linq;
using System.Threading.Tasks;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Runtime.Services;
using FastEndpoints;

namespace Elsa.Workflows.Api.Mappers;

public class WorkflowDefinitionMapper : ResponseMapper<WorkflowDefinitionResponse, WorkflowDefinition>
{
    public override async Task<WorkflowDefinitionResponse> FromEntityAsync(WorkflowDefinition entity)
    {
        var workflowDefinitionService = Resolve<IWorkflowDefinitionService>();
        var variableDefinitionMapper = Resolve<VariableDefinitionMapper>();
        var workflow = await workflowDefinitionService.MaterializeWorkflowAsync(entity);
        var variables = variableDefinitionMapper.Map(workflow.Variables).ToList();
        
        return new (
            entity.Id,
            entity.DefinitionId,
            entity.Name,
            entity.Description,
            entity.CreatedAt,
            entity.Version,
            variables,
            entity.Metadata,
            entity.ApplicationProperties,
            entity.IsLatest,
            entity.IsPublished,
            workflow.Root);   
    }
}
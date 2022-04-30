using Elsa.Management.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Implementations;

public class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IEnumerable<IWorkflowMaterializer> _materializers;

    public WorkflowDefinitionService(IEnumerable<IWorkflowMaterializer> materializers)
    {
        _materializers = materializers;
    }
    
    public async Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var provider = _materializers.FirstOrDefault(x => x.Name == definition.MaterializerName);

        if (provider == null)
            throw new Exception("Provider not found");

        return await provider.MaterializeAsync(definition, cancellationToken);
    }
}
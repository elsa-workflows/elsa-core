using Elsa.Builders;
using Elsa.Models;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Runtime.Options;
using Elsa.Runtime.Services;
using Elsa.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Runtime.WorkflowProviders;

/// <summary>
/// Provides workflows to the system that are registered with <see cref="WorkflowRuntimeOptions"/>
/// </summary>
public class ConfigurationWorkflowProvider : IWorkflowProvider
{
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkflowRuntimeOptions _options;
    private readonly ICollection<Workflow> _workflows;

    public ConfigurationWorkflowProvider(IOptions<WorkflowRuntimeOptions> options, IIdentityGraphService identityGraphService, IServiceProvider serviceProvider)
    {
        _identityGraphService = identityGraphService;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _workflows = CreateWorkflowDefinitions().ToList();
    }

    public ValueTask<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var result = _workflows.FirstOrDefault(x => x.Identity.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return ValueTask.FromResult(result);
    }

    public ValueTask<Workflow?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var result = _workflows.FirstOrDefault(x => x.WorkflowMetadata.Name == name && x.WithVersion(versionOptions));
        return ValueTask.FromResult(result);
    }

    public IAsyncEnumerable<Workflow> StreamAllAsync(CancellationToken cancellationToken = default) => _workflows.ToAsyncEnumerable();

    private IEnumerable<Workflow> CreateWorkflowDefinitions() => _options.Workflows.Values.Select(BuildWorkflowDefinition).ToList();

    private Workflow BuildWorkflowDefinition(Func<IServiceProvider, IWorkflow> workflowFactory)
    {
        var builder = new WorkflowDefinitionBuilder();
        var definition = workflowFactory(_serviceProvider);
        
        builder.WithDefinitionId(definition.GetType().Name);
        definition.Build(builder);
        
        var workflow = builder.BuildWorkflow();
        _identityGraphService.AssignIdentities(workflow);

        return workflow;
    }
}
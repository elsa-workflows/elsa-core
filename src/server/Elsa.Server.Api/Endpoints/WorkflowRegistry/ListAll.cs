using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Providers.Workflows;
using Elsa.Server.Api.Helpers;
using Elsa.Server.Api.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowRegistry;

[ApiController]
[ApiVersion("1")]
[Route("v{apiVersion:apiVersion}/workflow-registry")]
[Produces("application/json")]
public class ListAll : Controller
{
    private readonly IEnumerable<IWorkflowProvider> _workflowProviders;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IMapper _mapper;

    public ListAll(IEnumerable<IWorkflowProvider> workflowProviders, ITenantAccessor tenantAccessor, IMapper mapper)
    {
        _workflowProviders = workflowProviders;
        _tenantAccessor = tenantAccessor;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WorkflowBlueprintSummaryModel>))]
    [SwaggerOperation(
        Summary = "Returns a list of workflow blueprints.",
        Description = "Returns a list of workflow blueprints. When no version options are specified, the latest version is returned.",
        OperationId = "WorkflowBlueprints.List",
        Tags = new[] { "WorkflowBlueprints" })
    ]
    public async Task<IActionResult> Handle(VersionOptions? version = default, CancellationToken cancellationToken = default)
    {
        version ??= VersionOptions.Latest;
        var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
        var workflowBlueprints = await ListAllAsync(tenantId, version.Value, cancellationToken).OrderBy(x => x.DisplayName ?? x.Name ?? "(Untitled)").ToListAsync(cancellationToken);
        var model = _mapper.Map<IEnumerable<WorkflowBlueprintSummaryModel>>(workflowBlueprints).ToList();

        return Ok(model).ConfigureForWorkflowDefinition();
    }

    private async IAsyncEnumerable<IWorkflowBlueprint> ListAllAsync(string? tenantId, VersionOptions versionOptions, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var workflowProvider in _workflowProviders)
        {
            var workflowBlueprints = workflowProvider.ListAsync(versionOptions, tenantId: tenantId, cancellationToken: cancellationToken);

            await foreach (var workflowBlueprint in workflowBlueprints)
                yield return workflowBlueprint;
        }
    }
}

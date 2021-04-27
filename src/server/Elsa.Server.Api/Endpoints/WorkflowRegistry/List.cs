using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Server.Api.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Open.Linq.AsyncExtensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowRegistry
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-registry")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _serviceProvider;

        public List(IWorkflowRegistry workflowRegistry, IWorkflowBlueprintReflector workflowBlueprintReflector, IMapper mapper, IServiceProvider serviceProvider)
        {
            _workflowRegistry = workflowRegistry;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedList<WorkflowBlueprintSummaryModel>))]
        [SwaggerOperation(
            Summary = "Returns a list of workflow blueprints.",
            Description = "Returns paginated a list of workflow blueprints. When no version options are specified, the latest version is returned.",
            OperationId = "WorkflowBlueprints.List",
            Tags = new[] {"WorkflowBlueprints"})
        ]
        public async Task<ActionResult<PagedList<WorkflowBlueprintSummaryModel>>> Handle(int? page = default, int? pageSize = default, VersionOptions? version = default, CancellationToken cancellationToken = default)
        {
            version ??= VersionOptions.LatestOrPublished;
            var workflowBlueprints = await _workflowRegistry.FindManyAsync(x => x.WithVersion(version.Value), cancellationToken).ToList();
            var totalCount = workflowBlueprints.Count;
            var skip = page * pageSize;
            var items = workflowBlueprints.AsEnumerable();

            if (skip != null)
                items = items.Skip(skip.Value);

            if (pageSize != null)
                items = items.Take(pageSize.Value);

            using var scope = _serviceProvider.CreateScope();
            var mappedItems = _mapper.Map<IEnumerable<WorkflowBlueprintSummaryModel>>(items).ToList();
                
            return new PagedList<WorkflowBlueprintSummaryModel>(mappedItems, page, pageSize, totalCount);
        }
    }
}
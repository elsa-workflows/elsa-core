﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Activities
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/activities")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IActivityTypeService _activityTypeService;

        public List(IActivityTypeService activityTypeService)
        {
            _activityTypeService = activityTypeService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ActivityDescriptor>))]
        [SwaggerOperation(
            Summary = "Returns all available activities.",
            Description = "Returns all available activities from which workflow definitions can be built.",
            OperationId = "Activities.List",
            Tags = new[] { "Activities" })
        ]
        public async Task<IActionResult> Handle(CancellationToken cancellationToken)
        {
            var activityTypes = await _activityTypeService.GetActivityTypesAsync(cancellationToken);
            var tasks = activityTypes.Where(x => x.IsBrowsable).Select(x => DescribeActivity(x, cancellationToken)).ToList();
            var descriptors = await Task.WhenAll(tasks);
            return Json(descriptors);
        }

        private async Task<ActivityDescriptor> DescribeActivity(ActivityType activityType, CancellationToken cancellationToken) => await _activityTypeService.DescribeActivityType(activityType, cancellationToken);
    }
}
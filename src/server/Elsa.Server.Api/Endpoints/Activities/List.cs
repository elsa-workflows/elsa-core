using System.Collections.Generic;
using System.Linq;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.Activities
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/activities")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IEnumerable<IActivity> _activities;
        private readonly IActivityDescriber _activityDescriber;

        public List(IEnumerable<IActivity> activities, IActivityDescriber activityDescriber)
        {
            _activities = activities;
            _activityDescriber = activityDescriber;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Models.List<ActivityDescriptor>))]
        [SwaggerOperation(
            Summary = "Returns all available activities.",
            Description = "Returns all available activities from which a workflow definition can be built.",
            OperationId = "Activities.List",
            Tags = new[] { "Activities" })
        ]
        public IActionResult Handle()
        {
            var descriptors = _activities.Select(DescribeActivity).ToList();
            var model = new Models.List<ActivityDescriptor>(descriptors);
            return Ok(model);
        }

        private ActivityDescriptor DescribeActivity(IActivity activity) => _activityDescriber.Describe(activity.GetType());
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ICollection<Type> _activityTypes;
        private readonly IActivityDescriber _activityDescriber;

        public List(ElsaOptions elsaOptions, IActivityDescriber activityDescriber)
        {
            _activityTypes = elsaOptions.ActivityTypes.ToList();
            _activityDescriber = activityDescriber;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ActivityInfo>))]
        [SwaggerOperation(
            Summary = "Returns all available activities.",
            Description = "Returns all available activities from which a workflow definition can be built.",
            OperationId = "Activities.List",
            Tags = new[] { "Activities" })
        ]
        public IActionResult Handle()
        {
            var descriptors = _activityTypes.Select(DescribeActivity).Where(x => x != null).Select(x => x!).ToList();
            var model = new List<ActivityInfo>(descriptors);
            return Ok(model);
        }

        private ActivityInfo? DescribeActivity(Type activityType) => _activityDescriber.Describe(activityType);
    }
}
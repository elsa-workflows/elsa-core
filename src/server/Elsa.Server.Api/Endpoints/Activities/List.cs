using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Metadata;
using Elsa.Serialization;
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
        private readonly IContentSerializer _contentSerializer;

        public List(IActivityTypeService activityTypeService, IContentSerializer contentSerializer)
        {
            _activityTypeService = activityTypeService;
            _contentSerializer = contentSerializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ActivityDescriptor>))]
        [SwaggerOperation(
            Summary = "Returns all available activities.",
            Description = "Returns all available activities from which workflow definitions can be built.",
            OperationId = "Activities.List",
            Tags = new[] { "Activities" })
        ]
        public async Task<IActionResult> Handle()
        {
            var activityTypes = await _activityTypeService.GetActivityTypesAsync();
            var descriptors = activityTypes.Select(DescribeActivity).Where(x => x != null && x.Browsable).Select(x => x!).ToList();
            return Json(descriptors, _contentSerializer.GetSettings());
        }

        private ActivityDescriptor? DescribeActivity(ActivityType activityType) => activityType.Describe();
    }
}
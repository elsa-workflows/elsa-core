using System.Collections.Generic;
using System.Linq;
using Elsa.Metadata;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Api.Endpoints.Activities
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/activities")]
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
        public IActionResult Handle()
        {
            var descriptors = _activities.Select(DescribeActivity).ToList();
            return Ok(new { Activities = descriptors });
        }

        private ActivityDescriptor DescribeActivity(IActivity activity) => _activityDescriber.Describe(activity.GetType());
    }
}
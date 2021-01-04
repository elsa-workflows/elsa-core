using Elsa.Models;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Services
{
    public class ActivityFactory : IActivityFactory
    {
        public ActivityInstance Instantiate(IActivityBlueprint activityBlueprint) => new(
            activityBlueprint.Id,
            activityBlueprint.Type,
            null,
            new JObject());
    }
}
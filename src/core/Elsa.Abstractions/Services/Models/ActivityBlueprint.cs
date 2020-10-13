using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Elsa.Services.Models
{
    public class ActivityBlueprint : IActivityBlueprint
    {
        public ActivityBlueprint()
        {
        }

        public ActivityBlueprint(
            Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> createActivity) =>
            CreateActivityAsync = createActivity;

        public ActivityBlueprint(
            string id,
            Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> createActivity)
        {
            Id = id;
            CreateActivityAsync = createActivity;
        }

        public string Id { get; set; } = default!;
        public string Type { get; set; } = default!;
        public JObject Data { get; set; } = new JObject();

        public Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> CreateActivityAsync { get; set; }
            = default!;
    }
}
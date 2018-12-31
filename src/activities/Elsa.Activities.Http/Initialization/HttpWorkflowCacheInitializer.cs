using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.AsyncInitialization;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
using Elsa.Persistence;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Specifications;

namespace Elsa.Activities.Http.Initialization
{
    public class HttpWorkflowCacheInitializer : IAsyncInitializer
    {
        private readonly IWorkflowStore workflowStore;
        private readonly IHttpWorkflowCache httpWorkflowCache;

        public HttpWorkflowCacheInitializer(IWorkflowStore workflowStore, IHttpWorkflowCache httpWorkflowCache)
        {
            this.workflowStore = workflowStore;
            this.httpWorkflowCache = httpWorkflowCache;
        }
        
        public async Task InitializeAsync()
        {
            var specification = new WorkflowStartsWithActivity(nameof(HttpRequestTrigger)).Or(new WorkflowIsBlockedOnActivity(nameof(HttpRequestTrigger)));
            var workflows = await workflowStore.GetManyAsync(specification, CancellationToken.None); 
            foreach (var workflow in workflows)
            {
                var activities = new List<HttpRequestTrigger>();
                
                if (workflow.IsDefinition())
                {
                    var startActivities = workflow.GetStartActivities().Where(x => x is HttpRequestTrigger).Cast<HttpRequestTrigger>();
                    activities.AddRange(startActivities);
                }
                else
                {
                    var blockingActivities = workflow.BlockingActivities.Where(x => x is HttpRequestTrigger).Cast<HttpRequestTrigger>();
                    activities.AddRange(blockingActivities);
                }

                foreach (var activity in activities)
                {
                    await httpWorkflowCache.AddWorkflowAsync(activity.Path, workflow, CancellationToken.None);
                }
            }
        }
    }
}
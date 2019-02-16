using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Specifications;
using Elsa.Runtime;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class HttpRequestTriggerMiddleware
    {
        private readonly RequestDelegate next;

        public HttpRequestTriggerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, IWorkflowHost workflowHost, IWorkflowStore workflowStore)
        {
            var requestPath = new Uri(context.Request.Path.ToString(), UriKind.Relative);
            var cancellationToken = context.RequestAborted;
            var workflows = await GetWorkflowsByPathAsync(workflowStore, requestPath, cancellationToken).ToListAsync();

            if (!workflows.Any())
            {
                await next(context);
            }
            else
            {
                await InvokeWorkflows(workflowHost, workflows, cancellationToken);
            }
        }

        private async Task<IEnumerable<Tuple<Workflow, HttpRequestTrigger>>> GetWorkflowsByPathAsync(IWorkflowStore workflowStore, Uri path, CancellationToken cancellationToken)
        {
            var specification = new WorkflowStartsWithActivity(nameof(HttpRequestTrigger)).Or(new WorkflowIsBlockedOnActivity(nameof(HttpRequestTrigger)));
            var httpWorkflows = await workflowStore.GetManyAsync(specification, CancellationToken.None);

            var query =
                from workflow in httpWorkflows
                let activities = workflow.IsDefinition() ? workflow.GetStartActivities() : workflow.BlockingActivities
                let triggers = FilterByPath(activities, path)
                select triggers.Select(x => Tuple.Create(workflow, x)); 
            
            return query.SelectMany(x => x);
        }

        private IEnumerable<HttpRequestTrigger> FilterByPath(IEnumerable<IActivity> activities, Uri path)
        {
            return activities.Where(x => x is HttpRequestTrigger trigger && trigger.Path == path).Cast<HttpRequestTrigger>();
        }

        private async Task InvokeWorkflows(IWorkflowHost workflowHost, IEnumerable<Tuple<Workflow, HttpRequestTrigger>> workflows, CancellationToken cancellationToken)
        {
            foreach (var workflow in workflows)
            {
                await InvokeWorkflowAsync(workflowHost, workflow, cancellationToken);
            }
        }

        private async Task InvokeWorkflowAsync(IWorkflowHost workflowHost, Tuple<Workflow, HttpRequestTrigger> workflowTuple, CancellationToken cancellationToken)
        {
            var workflow = workflowTuple.Item1;
            var activity = workflowTuple.Item2;

            if (workflow.Status == WorkflowStatus.Idle)
            {
                await workflowHost.StartWorkflowAsync(workflow, activity, Variables.Empty, cancellationToken);
            }
            else if (workflow.Status == WorkflowStatus.Halted)
            {
                await workflowHost.ResumeWorkflowAsync(workflow, activity, Variables.Empty, cancellationToken);
            }
        }
    }
}
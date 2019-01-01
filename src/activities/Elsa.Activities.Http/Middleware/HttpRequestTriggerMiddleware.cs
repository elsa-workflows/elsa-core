using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
using Elsa.Models;
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

        public async Task InvokeAsync(HttpContext context, IWorkflowHost workflowHost, IHttpWorkflowCache httpWorkflowCache)
        {
            var requestPath = new Uri(context.Request.Path.ToString(), UriKind.Relative);
            var cancellationToken = context.RequestAborted;
            var workflows = await httpWorkflowCache.GetWorkflowsByPathAsync(requestPath, cancellationToken).ToListAsync();

            if (!workflows.Any())
            {
                await next(context);
            }
            else
            {
                await InvokeWorkflows(workflowHost, workflows, requestPath, cancellationToken);
            }
        }

        private async Task InvokeWorkflows(IWorkflowHost workflowHost, IEnumerable<Workflow> workflows, Uri requestPath, CancellationToken cancellationToken)
        {
            foreach (var workflow in workflows)
            {
                await InvokeWorkflowAsync(workflowHost, workflow, requestPath, cancellationToken);
            }
        }

        private async Task InvokeWorkflowAsync(IWorkflowHost workflowHost, Workflow workflow, Uri requestPath, CancellationToken cancellationToken)
        {
            if (workflow.Status == WorkflowStatus.Idle)
            {
                await StartHttpWorkflowAsync(workflowHost, workflow, requestPath, cancellationToken);
            }
            else if (workflow.Status == WorkflowStatus.Halted)
            {
                await ResumeHttpWorkflowAsync(workflowHost, workflow, requestPath, cancellationToken);
            }
        }
        
        private async Task StartHttpWorkflowAsync(IWorkflowHost workflowHost, Workflow workflow, Uri requestPath, CancellationToken cancellationToken)
        {
            var startActivity = GetActivityByRequestPath(workflow, requestPath);
            await workflowHost.StartWorkflowAsync(workflow, startActivity, Variables.Empty, cancellationToken);
        }

        private async Task ResumeHttpWorkflowAsync(IWorkflowHost workflowHost, Workflow workflow, Uri requestPath, CancellationToken cancellationToken)
        {
            var blockingActivity = GetActivityByRequestPath(workflow, requestPath);
            await workflowHost.ResumeWorkflowAsync(workflow, blockingActivity, Variables.Empty, cancellationToken);
        }
        
        private static HttpRequestTrigger GetActivityByRequestPath(Workflow workflow, Uri requestPath)
        {
            return (HttpRequestTrigger)workflow.Activities.Single(x => x is HttpRequestTrigger activity && activity.Path == requestPath);
        }
    }
}
using System;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Scripting;
using Elsa.Services.Models;
using Jint;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Http.Scripting
{
    public class HttpScriptEngineConfigurator : IScriptEngineConfigurator
    {
        private readonly ISharedAccessSignatureService sharedAccessSignatureService;
        private readonly IUrlHelper urlHelper;

        public HttpScriptEngineConfigurator(ISharedAccessSignatureService sharedAccessSignatureService, IUrlHelper urlHelper)
        {
            this.sharedAccessSignatureService = sharedAccessSignatureService;
            this.urlHelper = urlHelper;
        }
        
        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            engine.SetValue("signalUrl", (Func<string, string>) (signal => GenerateUrl(signal, workflowExecutionContext)));
        }

        private string GenerateUrl(string signal, WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstanceId = workflowExecutionContext.Workflow.Id;
            var activityId = workflowExecutionContext.CurrentActivity.Id;
            var payload = new Signal(signal, workflowInstanceId, activityId);
            var token = sharedAccessSignatureService.CreateToken(payload);
            var url = urlHelper.Action("Trigger", "HttpWorkflows", new { token });

            return url;
        }
    }
}
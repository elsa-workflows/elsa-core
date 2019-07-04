using System;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Scripting;
using Elsa.Services.Models;
using Jint;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Activities.Http.Scripting
{
    public class HttpScriptEngineConfigurator : IScriptEngineConfigurator
    {
        private readonly ISharedAccessSignatureService sharedAccessSignatureService;

        public HttpScriptEngineConfigurator(ISharedAccessSignatureService sharedAccessSignatureService)
        {
            this.sharedAccessSignatureService = sharedAccessSignatureService;
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
            var url = $"workflows/signal?token={token}";

            return url;
        }
    }
}
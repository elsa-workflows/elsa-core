using System;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Scripting;
using Elsa.Services.Models;
using Jint;

namespace Elsa.Activities.Http.Scripting
{
    public class HttpScriptEngineConfigurator : IScriptEngineConfigurator
    {
        private readonly ISharedAccessSignatureService sharedAccessSignatureService;
        private readonly IAbsoluteUrlProvider absoluteUrlProvider;

        public HttpScriptEngineConfigurator(ISharedAccessSignatureService sharedAccessSignatureService, IAbsoluteUrlProvider absoluteUrlProvider)
        {
            this.sharedAccessSignatureService = sharedAccessSignatureService;
            this.absoluteUrlProvider = absoluteUrlProvider;
        }
        
        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            engine.SetValue("signalUrl", (Func<string, string>) (signal => GenerateUrl(signal, workflowExecutionContext)));
        }

        private string GenerateUrl(string signal, WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstanceId = workflowExecutionContext.Workflow.Id;
            var payload = new Signal(signal, workflowInstanceId);
            var token = sharedAccessSignatureService.CreateToken(payload);
            var url = $"/workflows/signal?token={token}";

            return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}
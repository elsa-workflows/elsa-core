using System;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Scripting;
using Elsa.Scripting.JavaScript;
using Elsa.Services.Models;
using Jint;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Scripting
{
    public class HttpScriptEngineConfigurator : IScriptEngineConfigurator
    {
        private readonly ITokenService _tokenService;
        private readonly IAbsoluteUrlProvider absoluteUrlProvider;
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpScriptEngineConfigurator(
            ITokenService tokenService,
            IAbsoluteUrlProvider absoluteUrlProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            this._tokenService = tokenService;
            this.absoluteUrlProvider = absoluteUrlProvider;
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            engine.SetValue(
                "queryString",
                (Func<string, string>) (key => httpContextAccessor.HttpContext.Request.Query[key].ToString())
            );
            engine.SetValue(
                "absoluteUrl",
                (Func<string, string>) (url => absoluteUrlProvider.ToAbsoluteUrl(url).ToString())
            );
            engine.SetValue(
                "signalUrl",
                (Func<string, string>) (signal => GenerateUrl(signal, workflowExecutionContext))
            );
        }

        private string GenerateUrl(string signal, WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstanceId = workflowExecutionContext.Workflow.Id;
            var payload = new Signal(signal, workflowInstanceId);
            var token = _tokenService.CreateToken(payload);
            var url = $"/workflows/signal?token={token}";

            return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}
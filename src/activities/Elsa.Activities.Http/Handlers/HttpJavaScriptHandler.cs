using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Handlers
{
    public class HttpJavaScriptHandler : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly ITokenService tokenService;
        private readonly IAbsoluteUrlProvider absoluteUrlProvider;
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpJavaScriptHandler(
            ITokenService tokenService,
            IAbsoluteUrlProvider absoluteUrlProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            this.tokenService = tokenService;
            this.absoluteUrlProvider = absoluteUrlProvider;
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            var workflowExecutionContext = notification.WorkflowExecutionContext;

            engine.SetValue(
                "queryString",
                (Func<string, string>)(key => httpContextAccessor.HttpContext.Request.Query[key].ToString())
            );
            engine.SetValue(
                "absoluteUrl",
                (Func<string, string>)(url => absoluteUrlProvider.ToAbsoluteUrl(url).ToString())
            );
            engine.SetValue(
                "signalUrl",
                (Func<string, string>)(signal => GenerateUrl(signal, workflowExecutionContext))
            );

            return Task.CompletedTask;
        }

        private string GenerateUrl(string signal, WorkflowExecutionContext workflowExecutionContext)
        {
            var workflowInstanceId = workflowExecutionContext.Workflow.Id;
            var payload = new Signal(signal, workflowInstanceId);
            var token = tokenService.CreateToken(payload);
            var url = $"/workflows/signal?token={token}";

            return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}
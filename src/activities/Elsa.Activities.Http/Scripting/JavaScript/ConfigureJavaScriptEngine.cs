using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Extensions;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Activities.Http.Scripting.JavaScript
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>, INotificationHandler<RenderingTypeScriptDefinitions>
    {
        private readonly IAbsoluteUrlProvider _absoluteUrlProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfigureJavaScriptEngine(
            IAbsoluteUrlProvider absoluteUrlProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _absoluteUrlProvider = absoluteUrlProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            var activityExecutionContext = notification.ActivityExecutionContext;

            engine.SetValue(
                "queryString",
                (Func<string, string>)(key => _httpContextAccessor.HttpContext!.Request.Query[key].ToString())
            );
            engine.SetValue(
                "absoluteUrl",
                (Func<string, string>)(url => _absoluteUrlProvider.ToAbsoluteUrl(url).ToString())
            );
            engine.SetValue(
                "signalUrl",
                (Func<string, string>)(signal => activityExecutionContext.GenerateSignalUrl(signal))
            );
            engine.SetValue(
                "getRemoteIPAddress",
                (Func<string>)(() => _httpContextAccessor.HttpContext!.Connection.RemoteIpAddress.ToString())
            );
            engine.SetValue(
                "getRouteValue",
                (Func<string, object>)(key => _httpContextAccessor.HttpContext!.GetRouteValue(key))
            );

            return Task.CompletedTask;
        }

        public Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            var output = notification.Output;

            output.AppendLine("declare function queryString(name: string): string;");
            output.AppendLine("declare function absoluteUrl(url: string): string;");
            output.AppendLine("declare function signalUrl(signal: string): string;");
            output.AppendLine("declare function getRemoteIPAddress(): string;");
            output.AppendLine("declare function getRouteValue(name: string): any;");

            return Task.CompletedTask;
        }
    }
}
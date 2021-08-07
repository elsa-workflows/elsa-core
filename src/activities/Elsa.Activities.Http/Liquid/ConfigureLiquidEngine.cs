using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Fluid;
using Fluid.Values;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Http.Liquid
{
    public class ConfigureLiquidEngine : INotificationHandler<EvaluatingLiquidExpression>
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfigureLiquidEngine(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;
            var options = context.Options;

            options.Scope.SetValue("Request", new ObjectValue(new LiquidRequestAccessor()));

            options.MemberAccessStrategy.Register<HttpResponseModel>();
            options.MemberAccessStrategy.Register<HttpRequestModel>();
            options.MemberAccessStrategy.Register<LiquidRequestAccessor, FluidValue>((_, name, _) =>
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                if (request == null) return NilValue.Instance;
                return name switch
                {
                    nameof(HttpRequest.QueryString) => new StringValue(request.QueryString.Value),
                    nameof(HttpRequest.ContentType) => new StringValue(request.ContentType),
                    nameof(HttpRequest.ContentLength) => NumberValue.Create(request.ContentLength ?? 0),
                    nameof(HttpRequest.Form) => request.HasFormContentType ? new ObjectValue(request.Form) : NilValue.Instance,
                    nameof(HttpRequest.Protocol) => new StringValue(request.Protocol),
                    nameof(HttpRequest.Path) => new StringValue(request.Path.Value),
                    nameof(HttpRequest.PathBase) => new StringValue(request.PathBase.Value),
                    nameof(HttpRequest.Host) => new StringValue(request.Host.Value),
                    nameof(HttpRequest.IsHttps) => BooleanValue.Create(request.IsHttps),
                    nameof(HttpRequest.Scheme) => new StringValue(request.Scheme),
                    nameof(HttpRequest.Method) => new StringValue(request.Method),
                    _ => NilValue.Instance
                };
            });

            options.MemberAccessStrategy.Register<LiquidRequestAccessor, LiquidPropertyAccessor>(
                nameof(HttpRequest.Query), 
                _ => new LiquidPropertyAccessor(key => Task.FromResult<FluidValue>(new StringValue(_httpContextAccessor.HttpContext.Request.Query[key].ToString()))!));

            return Task.CompletedTask;
        }
    }
}
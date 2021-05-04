using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Http.Extensions
{
    public static class ActivityExecutionContextExtensions
    {
        public static string GenerateSignalUrl(this ActivityExecutionContext context, string signal)
        {
            var workflowInstanceId =
                context.WorkflowExecutionContext.WorkflowInstance.Id;
            
            var payload = new Signal(signal, workflowInstanceId);
            var tokenService = context.GetService<ITokenService>();
            var token = tokenService.CreateToken(payload);
            var url = $"/signals/trigger/{token}";
            var absoluteUrlProvider = context.GetService<IAbsoluteUrlProvider>();
            return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}
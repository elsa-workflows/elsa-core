using Elsa.Activities.Signaling.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Signaling.Extensions
{
    public static class ActivityExecutionContextExtensions
    {
        public static string GenerateSignalToken(this ActivityExecutionContext context, string signal)
        {
            var workflowInstanceId = context.WorkflowExecutionContext.WorkflowInstance.Id;
            var payload = new SignalModel(signal, workflowInstanceId);
            var tokenService = context.GetService<ITokenService>();
            return tokenService.CreateToken(payload);
        }
    }
}
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Signaling.Extensions;
using Elsa.Services.Models;

namespace Elsa.Activities.Http.Extensions
{
    public static class ActivityExecutionContextExtensions
    {
        public static string GenerateSignalUrl(this ActivityExecutionContext context, string signal)
        {
            var token = context.GenerateSignalToken(signal);
            var url = $"/signals/trigger/{token}";
            var absoluteUrlProvider = context.GetService<IAbsoluteUrlProvider>();
            return absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}
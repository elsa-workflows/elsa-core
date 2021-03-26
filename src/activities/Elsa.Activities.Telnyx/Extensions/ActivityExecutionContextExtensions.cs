using System;
using Elsa.Activities.Telnyx.Options;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Extensions
{
    public static class ActivityExecutionContextExtensions
    {
        /// <summary>
        /// Returns the default call control app ID if the specified value is null or empty.
        /// </summary>
        public static string GetCallControlAppId(this ActivityExecutionContext context, string? callControlId)
        {
            if (!string.IsNullOrWhiteSpace(callControlId))
                return callControlId;

            var options = context.GetService<TelnyxOptions>();
            callControlId = options.CallControlAppId;

            if (!string.IsNullOrWhiteSpace(callControlId))
                return callControlId;

            throw new Exception("No Call Control ID specified and no default value configured");
        }
    }
}
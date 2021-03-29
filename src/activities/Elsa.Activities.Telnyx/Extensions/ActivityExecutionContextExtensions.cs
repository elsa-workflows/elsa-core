using Elsa.Activities.Telnyx.Exceptions;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Extensions
{
    public static class ActivityExecutionContextExtensions
    {
        private const string CallControlIdVariableName = "TelnyxCallControlId";
        private const string FromNumberVariableName = "TelnyxFromNumber";
        
        /// <summary>
        /// Returns the control ID of the active call session in the workflow if the specified call control ID is null or empty.
        /// </summary>
        public static string GetCallControlId(this ActivityExecutionContext context, string? callControlId)
        {
            if (!string.IsNullOrWhiteSpace(callControlId))
                return callControlId;

            callControlId = context.GetVariable<string>(CallControlIdVariableName);

            if (!string.IsNullOrWhiteSpace(callControlId))
                return callControlId;

            throw new MissingCallControlIdException("No Call Control ID specified");
        }
        
        /// <summary>
        /// Sets a workflow variable with the specified call control ID value.
        /// </summary>
        public static void SetCallControlId(this ActivityExecutionContext context, string callControlId) => context.SetVariable(CallControlIdVariableName, callControlId);

        public static bool HasCallControlId(this ActivityExecutionContext context) => context.HasVariable(CallControlIdVariableName);
        
        /// <summary>
        /// Returns the control ID of the active call session in the workflow if the specified call control ID is null or empty.
        /// </summary>
        public static string GetFromNumber(this ActivityExecutionContext context, string? fromNumber)
        {
            if (!string.IsNullOrWhiteSpace(fromNumber))
                return fromNumber;

            fromNumber = context.GetVariable<string>(FromNumberVariableName);

            if (!string.IsNullOrWhiteSpace(fromNumber))
                return fromNumber;

            throw new MissingFromNumberException("No From Number specified");
        }
        
        /// <summary>
        /// Sets a workflow variable with the specified call control ID value.
        /// </summary>
        public static void SetFromNumber(this ActivityExecutionContext context, string fromNumber) => context.SetVariable(FromNumberVariableName, fromNumber);

        public static bool HasFromNumber(this ActivityExecutionContext context) => context.HasVariable(FromNumberVariableName);
    }
}
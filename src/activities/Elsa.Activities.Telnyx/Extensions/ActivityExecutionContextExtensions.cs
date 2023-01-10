using Elsa.Activities.Telnyx.Exceptions;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Extensions
{
    public static class ActivityExecutionContextExtensions
    {
        private const string CallControlIdVariableName = "TelnyxCallControlId";
        private const string CallLegIdVariableName = "TelnyxCallLegId";
        private const string FromNumberVariableName = "TelnyxFromNumber";
        private const string CallerNumberVariableName = "TelnyxCallerNumber";

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

        public static bool HasCallControlId(this ActivityExecutionContext context) => !string.IsNullOrWhiteSpace(context.GetVariable(CallControlIdVariableName) as string);

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
        public static void SetFromNumber(this ActivityExecutionContext context, string number) => context.SetVariable(FromNumberVariableName, number);

        public static void SetCallerNumber(this ActivityExecutionContext context, string number) => context.SetVariable(CallerNumberVariableName, number);
        public static string? GetCallerNumber(this ActivityExecutionContext context) => context.GetVariable<string>(CallerNumberVariableName);

        public static bool HasFromNumber(this ActivityExecutionContext context) => context.HasVariable(FromNumberVariableName);
        
        public static string GetCallLegId(this ActivityExecutionContext context, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = context.GetVariable<string>(CallLegIdVariableName);

            if (!string.IsNullOrWhiteSpace(value))
                return value;

            throw new MissingCallControlIdException("No Call Leg ID specified");
        }

        /// <summary>
        /// Sets a workflow variable with the specified call control ID value.
        /// </summary>
        public static void SetCallLegId(this ActivityExecutionContext context, string value) => context.SetVariable(CallLegIdVariableName, value);
        
        public static bool HasCallLegId(this ActivityExecutionContext context) => !string.IsNullOrWhiteSpace(context.GetVariable(CallLegIdVariableName) as string);
    }
}
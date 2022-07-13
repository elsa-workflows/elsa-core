using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;
using Elsa.Services.Models;
using Fluid;
using Fluid.Ast;

namespace Elsa.Activities.Telnyx.Scripting.Liquid
{
    public static class TelnyxClientStateTag
    {
        public static async ValueTask<Completion> WriteToAsync(TextWriter writer, TextEncoder encoder, TemplateContext context)
        {
            var activityExecutionContext = (ActivityExecutionContext)context.Model.ToObjectValue();
            var correlationId = activityExecutionContext.CorrelationId!;
            var clientState = new ClientStatePayload(correlationId);
            await writer.WriteLineAsync(clientState.ToBase64());
            return Completion.Normal;
        }
    }
}
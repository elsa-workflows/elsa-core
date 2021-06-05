using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Scripting.Liquid.Messages;
using Fluid;
using MediatR;

namespace Elsa.Activities.Telnyx.Scripting.Liquid
{
    public class ConfigureLiquidEngine : INotificationHandler<EvaluatingLiquidExpression>
    {
        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;
            var options = context.Options;
            var memberAccessStrategy = options.MemberAccessStrategy;

            memberAccessStrategy.Register<Extension>();
            memberAccessStrategy.Register<CallAnsweredPayload>();
            memberAccessStrategy.Register<CallBridgedPayload>();
            memberAccessStrategy.Register<CallDtmfReceivedPayload>();
            memberAccessStrategy.Register<CallGatherEndedPayload>();
            memberAccessStrategy.Register<CallHangupPayload>();
            memberAccessStrategy.Register<CallInitiatedPayload>();
            memberAccessStrategy.Register<CallPayload>();
            memberAccessStrategy.Register<CallPlayback>();
            memberAccessStrategy.Register<CallPlaybackEndedPayload>();
            memberAccessStrategy.Register<CallPlaybackStartedPayload>();
            memberAccessStrategy.Register<CallRecordingSaved>();
            memberAccessStrategy.Register<CallRecordingUrls>();
            memberAccessStrategy.Register<CallSpeakStarted>();
            memberAccessStrategy.Register<CallSpeakEnded>();
            
            memberAccessStrategy.Register<DialResponse>();
            memberAccessStrategy.Register<ErrorResponse>();
            memberAccessStrategy.Register<Error>();

            memberAccessStrategy.Register<TelnyxRecord>();
            memberAccessStrategy.Register<TelnyxWebhook>();
            memberAccessStrategy.Register<TelnyxWebhookData>();
            memberAccessStrategy.Register<TelnyxWebhookMeta>();
            memberAccessStrategy.Register<ClientStatePayload>();
            
            return Task.CompletedTask;
        }
    }
}
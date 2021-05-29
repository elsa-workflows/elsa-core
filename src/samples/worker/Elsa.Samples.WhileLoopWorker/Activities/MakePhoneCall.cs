using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Temporal;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Samples.WhileLoopWorker.Services;
using Elsa.Services;
using NodaTime;

namespace Elsa.Samples.WhileLoopWorker.Activities
{
    public class MakePhoneCall : CompositeActivity
    {
        private readonly PhoneCallService _phoneCallService;

        [ActivityInput]
        public string PhoneNumber
        {
            get => GetState<string>();
            set => SetState(value);
        }

        public MakePhoneCall(PhoneCallService phoneCallService)
        {
            _phoneCallService = phoneCallService;
        }

        public override void Build(ICompositeActivityBuilder builder)
        {
            builder
                .While(() => _phoneCallService.CallStatus != PhoneCallStatus.Finished,
                    @while =>
                    {
                        @while
                            .WriteLine(() => $"Ringgggg ringgg. {PhoneNumber}")
                            .Timer(Duration.FromSeconds(5))
                            .Then(() => _phoneCallService.Progress())
                            .WriteLine(() => $"Call status: {_phoneCallService.CallStatus}");
                    });
        }
    }
}
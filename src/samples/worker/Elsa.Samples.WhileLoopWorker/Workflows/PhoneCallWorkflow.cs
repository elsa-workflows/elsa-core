using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Samples.WhileLoopWorker.Services;
using NodaTime;

namespace Elsa.Samples.WhileLoopWorker.Workflows
{
    /// <summary>
    /// This workflow prompts the user to enter an integer start value, then iterates back from that value to 0.
    /// The workflow also demonstrates retrieving runtime values such as user input. 
    /// </summary>
    public class PhoneCallWorkflow : IWorkflow
    {
        private readonly PhoneCallService _phoneCallService;

        public PhoneCallWorkflow(PhoneCallService phoneCallService)
        {
            _phoneCallService = phoneCallService;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("Simulating a phone call... ringgg ringgg.")
                .While(() => _phoneCallService.CallStatus != PhoneCallStatus.Finished,
                    @while =>
                    {
                        @while
                            .WriteLine("Ringgggg ringgg.")
                            .WriteLine(() => $"Call status: {_phoneCallService.CallStatus}")
                            .TimerEvent(Duration.FromSeconds(5))
                            .Then(() => _phoneCallService.Progress());

                    })
                .WriteLine(() => $"Call status: {_phoneCallService.CallStatus}")
                .WriteLine("Workflow finished.");
        }
    }
}
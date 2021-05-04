using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.WhileLoopWorker.Activities;
using Elsa.Samples.WhileLoopWorker.Services;

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
        
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Simulating a phone call...")
                .Then<MakePhoneCall>()
                .WriteLine("Workflow finished.");
        }
    }
}
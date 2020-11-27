namespace Elsa.Samples.WhileLoopWorker.Services
{
    public class PhoneCallService
    {
        public PhoneCallStatus CallStatus { get; set; }

        public void Progress() =>
            CallStatus = CallStatus switch
            {
                PhoneCallStatus.Idle => PhoneCallStatus.Dialing,
                PhoneCallStatus.Dialing => PhoneCallStatus.InProgress,
                PhoneCallStatus.InProgress => PhoneCallStatus.Finished,
                _ => CallStatus
            };
    }

    public enum PhoneCallStatus
    {
        Idle,
        Dialing,
        InProgress,
        Finished
    }
}
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public class TriggeredSignal
    {
        public TriggeredSignal(string signalName, object? input)
        {
            SignalName = signalName;
            Input = input;
        }
        
        public string SignalName { get; set; }
        public object? Input { get; set; }
    }
}
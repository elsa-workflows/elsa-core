namespace Elsa.Activities.Signaling.Models
{
    public class Signal
    {
        public Signal(string signalName, object? input = default)
        {
            SignalName = signalName;
            Input = input;
        }
        
        public string SignalName { get; set; }
        public object? Input { get; set; }
    }
    
    
}
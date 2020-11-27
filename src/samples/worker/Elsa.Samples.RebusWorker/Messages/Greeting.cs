namespace Elsa.Samples.RebusWorker.Messages
{
    public class Greeting
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Message { get; set; }
        public override string ToString() => $"{From} says \"{Message}\" to {To}.";
    }
}
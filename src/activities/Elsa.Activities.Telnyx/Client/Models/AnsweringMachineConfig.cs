namespace Elsa.Activities.Telnyx.Client.Models
{
    public record AnsweringMachineConfig
    {
        public int AfterGreetingSilenceMillis { get; init; } = 800;
        public int BetweenWordsSilenceMillis { get; init; } = 50;
        public int GreetingDurationMillis { get; init; } = 3500;
        public int GreetingTotalAnalysisTimeMillis { get; init; } = 5000;
        public int InitialSilenceMillis { get; init; } = 3500;
        public int MaximumNumberOfWords { get; init; } = 5;
        public int MaximumWordLengthMillis { get; init; } = 3500;
        public int SilenceThreshold { get; init; } = 256;
        public int TotalAnalysisTimeMillis { get; init; } = 3500;
    }
}
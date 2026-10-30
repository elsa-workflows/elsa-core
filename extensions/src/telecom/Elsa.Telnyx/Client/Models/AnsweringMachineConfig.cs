namespace Elsa.Telnyx.Client.Models;

public record AnsweringMachineConfig(
    int AfterGreetingSilenceMillis = 800,
    int BetweenWordsSilenceMillis = 50,
    int GreetingDurationMillis = 3500,
    int GreetingTotalAnalysisTimeMillis = 5000,
    int InitialSilenceMillis = 3500,
    int MaximumNumberOfWords = 5,
    int MaximumWordLengthMillis = 3500,
    int SilenceThreshold = 256,
    int TotalAnalysisTimeMillis = 3500
);
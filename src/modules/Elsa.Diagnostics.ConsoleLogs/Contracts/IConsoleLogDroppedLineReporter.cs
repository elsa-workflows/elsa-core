namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

public interface IConsoleLogDroppedLineReporter
{
    void ReportDropped(ConsoleLogDroppedSummary summary);
}

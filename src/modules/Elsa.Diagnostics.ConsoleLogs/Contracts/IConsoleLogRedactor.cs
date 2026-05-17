namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

public interface IConsoleLogRedactor
{
    ConsoleLogLine Redact(ConsoleLogLine line);

    ConsoleLogSource Redact(ConsoleLogSource source);
}

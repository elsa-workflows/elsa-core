namespace Elsa.Workflows.Exceptions;

public class InputEvaluationException(string inputName, string message, Exception exception) : Exception(message, exception)
{
    public string InputName { get; } = inputName;
}
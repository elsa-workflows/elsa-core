namespace Elsa.Expressions.Models;

/// <summary>
/// A simple monad that runs either the <see cref="OnSuccess"/> or <see cref="OnFailure"/> lambda, depending on whether or not the operation succeeded.
/// </summary>
public class Result
{
    internal Result(bool success, object? value, Exception? exception)
    {
        Success = success;
        Value = value;
        Exception = exception;
    }

    /// <summary>
    /// True if the conversaion succeeded, false otherwise.
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// The result value.
    /// </summary>
    public object? Value { get; }
    
    /// <summary>
    /// Any exception that may have occurred during the operation.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Runs the provided delegate if the result is successful.
    /// </summary>
    public Result OnSuccess(Action<object?> successHandler)
    {
        if (Success)
            successHandler(Value);

        return this;
    }

    /// <summary>
    /// Runs the provided delegate if the result is unsuccessful.
    /// </summary>
    public Result OnFailure(Action<Exception> failureHandler)
    {
        if (Exception != null)
            failureHandler(Exception);

        return this;
    }
}
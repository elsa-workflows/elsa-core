namespace Elsa.Common.Models;

/// <summary>
/// A strongly typed monad that runs either the <see cref="OnSuccess"/> or <see cref="OnFailure"/> lambda, depending on whether or not the operation succeeded.
/// </summary>
public class Result<T>(bool success, T? value, Exception? exception)
{
    /// <summary>
    /// True if the conversion succeeded, false otherwise.
    /// </summary>
    public bool IsSuccess { get; } = success;

    /// <summary>
    /// The result value. Throws an exception if accessed when the result is a failure.
    /// </summary>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("Cannot access Value on a failed result. Check IsSuccess first or use ValueOrDefault.", Exception);
            return value!;
        }
    }

    /// <summary>
    /// The result value, or null/default if the result is a failure. Useful when null is a valid success value.
    /// </summary>
    public T? ValueOrDefault => value;

    /// <summary>
    /// Any exception that may have occurred during the operation.
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <summary>
    /// Runs the provided delegate if the result is successful.
    /// </summary>
    public Result<T> OnSuccess(Action<T> successHandler)
    {
        if (IsSuccess)
            successHandler(Value);

        return this;
    }

    /// <summary>
    /// Runs the provided async delegate if the result is successful.
    /// </summary>
    public async Task<Result<T>> OnSuccessAsync(Func<T, Task> successHandler)
    {
        if (IsSuccess)
            await successHandler(Value);

        return this;
    }

    /// <summary>
    /// Runs the provided delegate if the result is unsuccessful.
    /// </summary>
    public Result<T> OnFailure(Action<Exception> failureHandler)
    {
        if (Exception != null)
            failureHandler(Exception);

        return this;
    }

    /// <summary>
    /// Runs the provided async delegate if the result is unsuccessful.
    /// </summary>
    public async Task<Result<T>> OnFailureAsync(Func<Exception, Task> failureHandler)
    {
        if (Exception != null)
            await failureHandler(Exception);

        return this;
    }

    /// <summary>
    /// Throws the exception if the result is a failure.
    /// </summary>
    public Result<T> ThrowIfFailure()
    {
        if (!IsSuccess && Exception != null)
            throw Exception;

        return this;
    }
}

/// <summary>
/// A simple monad that runs either the <see cref="Result{T}.OnSuccess"/> or <see cref="Result{T}.OnFailure"/> lambda, depending on whether or not the operation succeeded.
/// </summary>
public class Result(bool success, object? value, Exception? exception) : Result<object>(success, value, exception)
{
    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success<T>(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result with the specified exception.
    /// </summary>
    public static Result<T> Failure<T>(Exception exception) => new(false, default, exception);
}
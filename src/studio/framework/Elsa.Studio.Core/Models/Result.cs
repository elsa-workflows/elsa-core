namespace Elsa.Studio.Models;

/// <summary>
/// Represents a result that is either successful or failed.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TFailure">The type of the failure value.</typeparam>
public class Result<TSuccess, TFailure>
{
    /// <summary>
    /// Creates a new successful result.
    /// </summary>
    public Result(TSuccess success)
    {
        Success = success;
    }

    /// <summary>
    /// Creates a new failed result.
    /// </summary>
    /// <param name="failure"></param>
    public Result(TFailure failure)
    {
        Failure = failure;
    }

    /// <summary>
    /// The success value.
    /// </summary>
    public TSuccess? Success { get; set; }

    /// <summary>
    /// The failure value.
    /// </summary>
    public TFailure? Failure { get; set; }

    /// <summary>
    /// Returns true if the result is successful.
    /// </summary>
    public bool IsSuccess => Success is not null;

    /// <summary>
    /// Returns true if the result is failed.
    /// </summary>
    public bool IsFailed => Failure is not null;

    /// <summary>
    /// Invokes the specified handler if the result is successful.
    /// </summary>
    /// <returns>The success result.</returns>
    public async Task<TSuccess> OnSuccessAsync(Func<TSuccess, Task> handler)
    {
        if (!IsSuccess) return default!;
        await handler(Success!);
        return Success!;
    }

    /// <summary>
    /// Invokes the specified handler if the result is successful.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TSuccess OnSuccess(Action<TSuccess> handler)
    {
        if (!IsSuccess) return default!;
        handler(Success!);
        return Success!;
    }

    /// <summary>
    /// Invokes the specified handler if the result is failed.
    /// </summary>
    /// <returns>The failed result.</returns>
    public async Task<TFailure> OnFailedAsync(Func<TFailure, Task> handler)
    {
        if (!IsFailed) return default!;
        await handler(Failure!);
        return Failure!;
    }

    /// <summary>
    /// Invokes the specified handler if the result is failed.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TFailure OnFailed(Action<TFailure> handler)
    {
        if (!IsFailed) return default!;
        handler(Failure!);
        return Failure!;
    }
}
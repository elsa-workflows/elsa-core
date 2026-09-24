using System.Reflection;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Models;

namespace Elsa.Connections.Credentials.WorkerProcess;

public class ProcessBoundaryConnectionLifecycleStore : DispatchProxy
{
    private IConnectionLifecycleStore? _inner;

    public void Initialize(IConnectionLifecycleStore inner) => _inner = inner;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null || _inner is null)
        {
            throw new InvalidOperationException("lifecycle_store_proxy_uninitialized");
        }

        var methodName = targetMethod.Name;
        var barrierParticipant = Environment.GetEnvironmentVariable("ELSA_TEST_BARRIER_METHOD") == methodName
            ? Environment.GetEnvironmentVariable("ELSA_TEST_BARRIER_PARTICIPANT") ?? methodName
            : null;
        var crashBoundary = GetCrashBoundary(methodName);
        if (barrierParticipant is null && crashBoundary is null)
        {
            return targetMethod.Invoke(_inner, args);
        }

        if (targetMethod.ReturnType == typeof(Task))
        {
            return InvokeTaskAsync(targetMethod, args ?? [], barrierParticipant, crashBoundary);
        }

        if (targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = targetMethod.ReturnType.GenericTypeArguments[0];
            return typeof(ProcessBoundaryConnectionLifecycleStore)
                .GetMethod(nameof(InvokeGenericTaskAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(resultType)
                .Invoke(this, [targetMethod, args ?? [], barrierParticipant, crashBoundary]);
        }

        throw new InvalidOperationException("unsupported_lifecycle_store_return_type");
    }

    private async Task InvokeTaskAsync(MethodInfo method, object?[] args, string? barrierParticipant, string? crashBoundary)
    {
        if (barrierParticipant is not null)
        {
            await WaitAtBarrierAsync(barrierParticipant);
        }

        await (Task)method.Invoke(_inner, args)!;
        if (crashBoundary is not null)
        {
            await ProcessBoundary.PauseAsync(crashBoundary);
        }
    }

    private async Task<TResult> InvokeGenericTaskAsync<TResult>(
        MethodInfo method,
        object?[] args,
        string? barrierParticipant,
        string? crashBoundary)
    {
        if (barrierParticipant is not null)
        {
            await WaitAtBarrierAsync(barrierParticipant);
        }

        var result = await (Task<TResult>)method.Invoke(_inner, args)!;
        if (crashBoundary is not null && IsSuccessfulResult(result))
        {
            await ProcessBoundary.PauseAsync(crashBoundary);
        }

        return result;
    }

    private static string? GetCrashBoundary(string methodName)
    {
        if (methodName == "TryClaimRefreshAsync")
        {
            return "refresh-claimed";
        }
        if (methodName == "TryStartProviderCallAsync")
        {
            return "refresh-provider-started";
        }
        if (methodName == "TryRecordStagedGenerationAsync")
        {
            return "refresh-staged";
        }
        if (methodName == "TryPublishGenerationAsync")
        {
            return "refresh-published";
        }

        return null;
    }

    private static bool IsSuccessfulResult<TResult>(TResult result) => result switch
    {
        bool value => value,
        null => false,
        _ => true
    };

    private static async Task WaitAtBarrierAsync(string participant)
    {
        var directory = Environment.GetEnvironmentVariable("ELSA_TEST_BARRIER_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("barrier_directory_missing");
        }

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Join(directory, $"{participant}.ready"), "ready");
        Console.WriteLine($"BARRIER_READY:{participant}");
        await Console.Out.FlushAsync();
        var releasePath = Path.Join(directory, "go");
        while (!File.Exists(releasePath))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
    }
}

public static class ProcessBoundary
{
    public static async Task PauseAsync(string boundary)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ELSA_TEST_CRASH_AFTER"), boundary, StringComparison.Ordinal))
        {
            return;
        }

        Console.WriteLine($"BOUNDARY:{boundary}");
        await Console.Out.FlushAsync();
        await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
    }
}

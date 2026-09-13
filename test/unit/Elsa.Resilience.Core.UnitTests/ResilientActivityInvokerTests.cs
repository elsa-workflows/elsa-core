using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Resilience.Core.UnitTests.TestHelpers;
using Elsa.Resilience.Extensions;
using Elsa.Resilience.Models;
using Elsa.Resilience.Options;
using Elsa.Resilience.Serialization;
using Elsa.Workflows;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilientActivityInvokerTests
{
    private readonly IResilienceStrategyConfigEvaluator _evaluator = Substitute.For<IResilienceStrategyConfigEvaluator>();
    private readonly IRetryAttemptRecorder _recorder = Substitute.For<IRetryAttemptRecorder>();
    private readonly IIdentityGenerator _identityGenerator = Substitute.For<IIdentityGenerator>();
    private readonly TestResilientActivity _activity = new();
    private readonly ResilientActivityInvoker _invoker;

    public ResilientActivityInvokerTests()
    {
        _identityGenerator.GenerateId().Returns(_ => Guid.NewGuid().ToString("N"));

        var options = Microsoft.Extensions.Options.Options.Create(new ResilienceOptions
        {
            StrategyTypes = [typeof(TestRetryStrategy)]
        });

        _invoker = new(_evaluator, _recorder, _identityGenerator, new ResilienceStrategySerializer(options));

        // Default to "no strategy configured"; tests that need one call SetupStrategy.
        SetupStrategy(null);
    }

    [Test]
    [DisplayName("Invoker without a strategy should run the action once and return its result")]
    public async Task InvokeAsync_NoStrategy_RunsActionOnce()
    {
        var context = await CreateContextAsync();
        var invocations = 0;

        var result = await _invoker.InvokeAsync(_activity, context, () =>
        {
            invocations++;
            return Task.FromResult("done");
        });

        await Assert.That(result).IsEqualTo("done");
        await Assert.That(invocations).IsEqualTo(1);
    }

    [Test]
    [DisplayName("Invoker without a strategy should leave the context untouched")]
    public async Task InvokeAsync_NoStrategy_DoesNotAnnotateContext()
    {
        var context = await CreateContextAsync();

        await _invoker.InvokeAsync(_activity, context, () => Task.FromResult("done"));

        await Assert.That(context.GetProperty<object>("ResilienceStrategy")).IsNull();
        await Assert.That(context.GetRetriesAttemptedFlag()).IsFalse();
        await _recorder.DidNotReceive().RecordAsync(Arg.Any<RecordRetryAttemptsContext>());
    }

    [Test]
    [DisplayName("Invoker should pass the activity's configured strategy config to the evaluator")]
    public async Task InvokeAsync_ActivityWithStrategyConfig_PassesConfigToEvaluator()
    {
        var context = await CreateContextAsync();
        _activity.CustomProperties["resilienceStrategy"] = new ResilienceStrategyConfig
        {
            Mode = ResilienceStrategyConfigMode.Identifier,
            StrategyId = "my-strategy"
        };

        await _invoker.InvokeAsync(_activity, context, () => Task.FromResult("done"));

        await _evaluator.Received(1).EvaluateAsync(
            Arg.Is<ResilienceStrategyConfig>(c => c.Mode == ResilienceStrategyConfigMode.Identifier && c.StrategyId == "my-strategy"),
            context.ExpressionExecutionContext,
            Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Invoker without a configured strategy should pass a null config to the evaluator")]
    public async Task InvokeAsync_ActivityWithoutStrategyConfig_PassesNullConfigToEvaluator()
    {
        var context = await CreateContextAsync();

        await _invoker.InvokeAsync(_activity, context, () => Task.FromResult("done"));

        await _evaluator.Received(1).EvaluateAsync(null, context.ExpressionExecutionContext, Arg.Any<CancellationToken>());
    }

    [Test]
    [DisplayName("Invoker with a strategy should record the applied strategy on the context")]
    public async Task InvokeAsync_WithStrategy_RecordsAppliedStrategyOnContext()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy { Id = "applied-strategy" });

        await _invoker.InvokeAsync(_activity, context, () => Task.FromResult("done"));

        var model = context.GetProperty<System.Text.Json.Nodes.JsonNode>("ResilienceStrategy");
        await Assert.That(model).IsNotNull();
        await Assert.That(model["$type"]!.GetValue<string>()).IsEqualTo(nameof(TestRetryStrategy));
        await Assert.That(model["id"]!.GetValue<string>()).IsEqualTo("applied-strategy");
    }

    [Test]
    [DisplayName("Invoker with a strategy should not record retry attempts when the action succeeds first time")]
    public async Task InvokeAsync_WithStrategy_ActionSucceedsImmediately_RecordsNoAttempts()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy());

        var result = await _invoker.InvokeAsync(_activity, context, () => Task.FromResult("done"));

        await Assert.That(result).IsEqualTo("done");
        await _recorder.DidNotReceive().RecordAsync(Arg.Any<RecordRetryAttemptsContext>());
        await Assert.That(context.GetRetriesAttemptedFlag()).IsFalse();
    }

    [Test]
    [DisplayName("Invoker should retry a transient failure and return the eventual result")]
    public async Task InvokeAsync_ActionFailsThenSucceeds_RetriesAndReturnsResult()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy { MaxRetryAttempts = 3 });
        var invocations = 0;

        var result = await _invoker.InvokeAsync(_activity, context, () =>
        {
            invocations++;
            if (invocations < 3) throw new InvalidOperationException($"boom {invocations}");
            return Task.FromResult("recovered");
        });

        await Assert.That(result).IsEqualTo("recovered");
        await Assert.That(invocations).IsEqualTo(3);
    }

    [Test]
    [DisplayName("Invoker should record one attempt per retry, with the failure detail attached")]
    public async Task InvokeAsync_ActionFailsThenSucceeds_RecordsOneAttemptPerRetry()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy { MaxRetryAttempts = 3 });
        var invocations = 0;
        RecordRetryAttemptsContext? recordContext = null;
        await _recorder.RecordAsync(Arg.Do<RecordRetryAttemptsContext>(c => recordContext = c));

        await _invoker.InvokeAsync(_activity, context, () =>
        {
            invocations++;
            if (invocations < 3) throw new InvalidOperationException($"boom {invocations}");
            return Task.FromResult("recovered");
        });

        await Assert.That(recordContext).IsNotNull();
        await Assert.That(recordContext!.ActivityExecutionContext).IsSameReferenceAs(context);
        await Assert.That(recordContext.Attempts.Select(x => (x.AttemptNumber, x.Details["exception"]))).IsEquivalentTo(
            [(0, "boom 1"), (1, "boom 2")],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Invoker should stamp recorded attempts with the activity and workflow identifiers")]
    public async Task InvokeAsync_ActionFailsThenSucceeds_StampsRecordsWithIdentifiers()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy());
        _identityGenerator.GenerateId().Returns("generated-id");
        RecordRetryAttemptsContext? recordContext = null;
        await _recorder.RecordAsync(Arg.Do<RecordRetryAttemptsContext>(c => recordContext = c));

        await _invoker.InvokeAsync(_activity, context, FailOnceThenSucceed());

        var record = await Assert.That(recordContext!.Attempts).HasSingleItem();
        await Assert.That(record.Id).IsEqualTo("generated-id");
        await Assert.That(record.ActivityInstanceId).IsEqualTo(context.Id);
        await Assert.That(record.ActivityId).IsEqualTo(context.Activity.Id);
        await Assert.That(record.WorkflowInstanceId).IsEqualTo(context.WorkflowExecutionContext.Id);
    }

    [Test]
    [DisplayName("Invoker should drop retry details whose value is null")]
    public async Task InvokeAsync_ActionFailsThenSucceeds_DropsNullDetails()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy());
        RecordRetryAttemptsContext? recordContext = null;
        await _recorder.RecordAsync(Arg.Do<RecordRetryAttemptsContext>(c => recordContext = c));

        await _invoker.InvokeAsync(_activity, context, FailOnceThenSucceed());

        var record = await Assert.That(recordContext!.Attempts).HasSingleItem();
        await Assert.That(record.Details.Keys).IsEquivalentTo(
            ["exception"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Invoker should flag that retries occurred and expose the attempt count")]
    public async Task InvokeAsync_ActionFailsThenSucceeds_FlagsRetriesAndExposesCount()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy { MaxRetryAttempts = 3 });

        await _invoker.InvokeAsync(_activity, context, FailOnceThenSucceed());

        await Assert.That(context.GetRetriesAttemptedFlag()).IsTrue();
        await Assert.That(context.GetExtensionsMetadata()!["RetryAttemptsCount"]).IsEqualTo(1);
    }

    [Test]
    [DisplayName("Invoker should surface the failure once the strategy stops retrying")]
    public async Task InvokeAsync_ActionAlwaysFails_ThrowsAfterExhaustingRetries()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy { MaxRetryAttempts = 2 });
        var invocations = 0;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _invoker.InvokeAsync<string>(_activity, context, () =>
        {
            invocations++;
            throw new InvalidOperationException("always boom");
        }));

        await Assert.That(invocations).IsEqualTo(3);
        await _recorder.DidNotReceive().RecordAsync(Arg.Any<RecordRetryAttemptsContext>());
    }

    [Test]
    [DisplayName("Invoker should not retry an exception the strategy does not handle")]
    public async Task InvokeAsync_UnhandledExceptionType_DoesNotRetry()
    {
        var context = await CreateContextAsync();
        SetupStrategy(new TestRetryStrategy { MaxRetryAttempts = 3 });
        var invocations = 0;

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => _invoker.InvokeAsync<string>(_activity, context, () =>
        {
            invocations++;
            throw new NotSupportedException("not transient");
        }));

        await Assert.That(invocations).IsEqualTo(1);
    }

    private static Func<Task<string>> FailOnceThenSucceed()
    {
        var invocations = 0;

        return () =>
        {
            invocations++;
            if (invocations == 1) throw new InvalidOperationException("boom 1");
            return Task.FromResult("recovered");
        };
    }

    private void SetupStrategy(IResilienceStrategy? strategy)
    {
        _evaluator.EvaluateAsync(Arg.Any<ResilienceStrategyConfig?>(), Arg.Any<ExpressionExecutionContext>(), Arg.Any<CancellationToken>()).Returns(strategy);
    }

    private Task<ActivityExecutionContext> CreateContextAsync() => ContextFactory.CreateAsync(_activity);
}

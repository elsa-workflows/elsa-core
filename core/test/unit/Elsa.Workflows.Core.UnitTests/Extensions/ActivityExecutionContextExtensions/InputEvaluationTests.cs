using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class InputEvaluationTests
{
    private readonly ActivityWithSensitiveInputs _activity = new();
    private readonly ActivityTestFixture _fixture;

    public InputEvaluationTests()
    {
        _fixture = new(_activity);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsSensitive_RemovesItFromActivityState()
    {
        _fixture.ConfigureContext(context => context.ActivityState[nameof(ActivityWithSensitiveInputs.SensitiveText)] = "stale-secret");

        var context = await ActivateAsync();

        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithSensitiveInputs.SensitiveText)));
        Assert.Equal("secret", _activity.CapturedSensitiveText);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsNotSensitive_StoresItInActivityState()
    {
        var context = await ActivateAsync();

        Assert.Equal("public", context.ActivityState[nameof(ActivityWithSensitiveInputs.PublicText)]);
        Assert.Equal("public", _activity.CapturedPublicText);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsBoundToSensitiveExpression_RemovesItFromActivityState()
    {
        _activity.PublicText = new(new Expression(SensitiveExpressionType, null));
        _fixture.ConfigureServices(services => services.AddSingleton<IExpressionDescriptorProvider, SensitiveExpressionDescriptorProvider>());

        var context = await ActivateAsync();

        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithSensitiveInputs.PublicText)));
        Assert.Equal(SensitiveExpressionValue, _activity.CapturedPublicText);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenResumingWithSensitiveExpressionInput_RemovesPreviouslyStoredValue()
    {
        _activity.PublicText = new(new Expression(SensitiveExpressionType, null));
        _fixture.ConfigureServices(services => services.AddSingleton<IExpressionDescriptorProvider, SensitiveExpressionDescriptorProvider>());
        _fixture.ConfigureContext(context => context.ActivityState[nameof(ActivityWithSensitiveInputs.PublicText)] = "stale-secret");

        var context = await ActivateAsync();

        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithSensitiveInputs.PublicText)));
        Assert.Equal(SensitiveExpressionValue, _activity.CapturedPublicText);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenInputIsBoundToNonSensitiveExpression_StoresItInActivityState()
    {
        _activity.PublicText = new(new Expression(PlainExpressionType, null));
        _fixture.ConfigureServices(services => services.AddSingleton<IExpressionDescriptorProvider, SensitiveExpressionDescriptorProvider>());

        var context = await ActivateAsync();

        Assert.Equal(SensitiveExpressionValue, context.ActivityState[nameof(ActivityWithSensitiveInputs.PublicText)]);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenCollectionEntryIsBoundToSensitiveExpression_RemovesItFromActivityState()
    {
        var activity = new ActivityWithInputCollections
        {
            Items = [new("public"), new(new Expression(SensitiveExpressionType, null))]
        };
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton<IExpressionDescriptorProvider, SensitiveExpressionDescriptorProvider>());

        var context = await fixture.ExecuteAsync();

        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithInputCollections.Items)));
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenDictionaryEntryIsBoundToSensitiveExpression_RemovesItFromActivityState()
    {
        var activity = new ActivityWithInputCollections
        {
            Map = new() { ["public"] = new("public"), ["secret"] = new(new Expression(SensitiveExpressionType, null)) }
        };
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton<IExpressionDescriptorProvider, SensitiveExpressionDescriptorProvider>());

        var context = await fixture.ExecuteAsync();

        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithInputCollections.Map)));
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenCollectionAndDictionaryEntriesAreNotSensitive_StoresEvaluatedValuesInActivityState()
    {
        var activity = new ActivityWithInputCollections
        {
            Items = [new("first"), new("second")],
            Map = new() { ["key"] = new("value") }
        };
        var fixture = new ActivityTestFixture(activity);

        var context = await fixture.ExecuteAsync();

        Assert.Equal(["first", "second"], Assert.IsType<List<object?>>(context.ActivityState[nameof(ActivityWithInputCollections.Items)]));
        Assert.Equal("value", Assert.IsType<Dictionary<string, object?>>(context.ActivityState[nameof(ActivityWithInputCollections.Map)])["key"]);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenCollectionOrDictionaryEntryHasNoExpression_EvaluatesItToNull()
    {
        var activity = new ActivityWithInputCollections
        {
            Items = [new("first"), null!, new((Expression)null!)],
            Map = new() { ["key"] = new("value"), ["missing"] = null!, ["empty"] = new((Expression)null!) }
        };
        var fixture = new ActivityTestFixture(activity);

        var context = await fixture.ExecuteAsync();

        Assert.Equal(["first", null, null], Assert.IsType<List<object?>>(context.ActivityState[nameof(ActivityWithInputCollections.Items)]));
        var map = Assert.IsType<Dictionary<string, object?>>(context.ActivityState[nameof(ActivityWithInputCollections.Map)]);
        Assert.Equal("value", map["key"]);
        Assert.Null(map["missing"]);
        Assert.Null(map["empty"]);
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenEntryHasNoMemoryBlockId_AssignsDeterministicId()
    {
        var activity = new ActivityWithInputCollections
        {
            Items = [new("first")],
            Map = new() { ["key"] = new("value") }
        };
        var fixture = new ActivityTestFixture(activity);
        var context = await fixture.BuildAsync();

        // Simulate entries created without identities, for example by an activity provider.
        activity.Items![0].MemoryBlockReference().Id = null!;
        activity.Map!["key"].MemoryBlockReference().Id = null!;
        await fixture.ExecuteAsync(context);

        Assert.Equal($"{activity.NodeId}.Items:1", activity.Items![0].MemoryBlockReference().Id);
        Assert.Equal($"{activity.NodeId}.Map:key", activity.Map!["key"].MemoryBlockReference().Id);
        Assert.Equal("first", context.Get(activity.Items![0]));
        Assert.Equal("value", context.Get(activity.Map!["key"]));
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_EvaluatesArraysSetsAndGenericOnlyDictionaries()
    {
        var activity = new ActivityWithOtherInputCollections
        {
            ArrayItems = [new("first"), new("second")],
            SetItems = [new("only")],
            Map = new() { ["key"] = new("value") }
        };
        var fixture = new ActivityTestFixture(activity);

        var context = await fixture.ExecuteAsync();

        Assert.Equal(["first", "second"], Assert.IsType<List<object?>>(context.ActivityState[nameof(ActivityWithOtherInputCollections.ArrayItems)]));
        Assert.Equal(["only"], Assert.IsType<List<object?>>(context.ActivityState[nameof(ActivityWithOtherInputCollections.SetItems)]));
        Assert.Equal("value", Assert.IsType<Dictionary<string, object?>>(context.ActivityState[nameof(ActivityWithOtherInputCollections.Map)])["key"]);
        Assert.Equal("second", context.Get(activity.ArrayItems[1]));
        Assert.Equal("value", context.Get(activity.Map!["key"]));
    }

    [Fact]
    public async Task EvaluateInputPropertiesAsync_WhenSetOrGenericOnlyDictionaryEntryIsSensitive_RemovesItFromActivityState()
    {
        var activity = new ActivityWithOtherInputCollections
        {
            SetItems = [new(new Expression(SensitiveExpressionType, null))],
            Map = new() { ["secret"] = new(new Expression(SensitiveExpressionType, null)) }
        };
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton<IExpressionDescriptorProvider, SensitiveExpressionDescriptorProvider>());

        var context = await fixture.ExecuteAsync();

        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithOtherInputCollections.SetItems)));
        Assert.False(context.ActivityState.ContainsKey(nameof(ActivityWithOtherInputCollections.Map)));
    }

    private const string PlainExpressionType = "PlainTest";
    private const string SensitiveExpressionType = "SensitiveTest";
    private const string SensitiveExpressionValue = "resolved-secret";

    private Task<ActivityExecutionContext> ActivateAsync() => _fixture.ExecuteAsync();

    private sealed class ActivityWithSensitiveInputs : CodeActivity
    {
        [Input(CanContainSecrets = true)]
        public Input<string?> SensitiveText { get; set; } = new("secret");

        [Input]
        public Input<string?> PublicText { get; set; } = new("public");

        public string? CapturedSensitiveText { get; private set; }
        public string? CapturedPublicText { get; private set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            CapturedSensitiveText = context.Get(SensitiveText);
            CapturedPublicText = context.Get(PublicText);
        }
    }

    private sealed class ActivityWithInputCollections : CodeActivity
    {
        public List<Input<string?>>? Items { get; set; }
        public Dictionary<string, Input<string?>>? Map { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }

    private sealed class ActivityWithOtherInputCollections : CodeActivity
    {
        public Input<string?>[] ArrayItems { get; set; } = [];
        public HashSet<Input<string?>> SetItems { get; set; } = [];
        public GenericOnlyDictionary<Input<string?>> Map { get; set; } = new();

        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }

    /// <summary>
    /// Implements only the generic <see cref="IDictionary{TKey,TValue}"/>, not the non-generic <see cref="System.Collections.IDictionary"/>.
    /// </summary>
    private sealed class GenericOnlyDictionary<TValue> : IDictionary<string, TValue>
    {
        private readonly Dictionary<string, TValue> _entries = new();

        public TValue this[string key] { get => _entries[key]; set => _entries[key] = value; }
        public ICollection<string> Keys => _entries.Keys;
        public ICollection<TValue> Values => _entries.Values;
        public int Count => _entries.Count;
        public bool IsReadOnly => false;
        public void Add(string key, TValue value) => _entries.Add(key, value);
        public void Add(KeyValuePair<string, TValue> item) => _entries.Add(item.Key, item.Value);
        public void Clear() => _entries.Clear();
        public bool Contains(KeyValuePair<string, TValue> item) => _entries.Contains(item);
        public bool ContainsKey(string key) => _entries.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, TValue>>)_entries).CopyTo(array, arrayIndex);
        public bool Remove(string key) => _entries.Remove(key);
        public bool Remove(KeyValuePair<string, TValue> item) => ((ICollection<KeyValuePair<string, TValue>>)_entries).Remove(item);
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value) => _entries.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => _entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SensitiveExpressionDescriptorProvider : IExpressionDescriptorProvider
    {
        public IEnumerable<ExpressionDescriptor> GetDescriptors()
        {
            yield return new()
            {
                Type = SensitiveExpressionType,
                DisplayName = "Sensitive test",
                IsSensitive = true,
                HandlerFactory = _ => new SensitiveExpressionHandler()
            };
            yield return new()
            {
                Type = PlainExpressionType,
                DisplayName = "Plain test",
                HandlerFactory = _ => new SensitiveExpressionHandler()
            };
        }
    }

    private sealed class SensitiveExpressionHandler : IExpressionHandler
    {
        public ValueTask<object?> EvaluateAsync(Expression expression, Type returnType, ExpressionExecutionContext context, ExpressionEvaluatorOptions options) =>
            ValueTask.FromResult<object?>(SensitiveExpressionValue);
    }
}

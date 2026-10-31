using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.UnitTests.Models;

public class CreateBookmarkArgsTests
{
    [Fact]
    public void NewCreateBookmarkArgs_IncludeActivityInstanceId_DefaultsToTrue()
    {
        var args = new CreateBookmarkArgs();

        Assert.True(args.IncludeActivityInstanceId);
    }

    [Fact]
    public void UnsetIncludeActivityInstanceId_InObjectInitializer_IsTrue()
    {
        var args = new CreateBookmarkArgs
        {
            Stimulus = "order-created"
        };

        Assert.True(args.IncludeActivityInstanceId);
    }

    [Fact]
    public void ExplicitFalse_IsPreserved()
    {
        var args = new CreateBookmarkArgs
        {
            Stimulus = "order-created",
            IncludeActivityInstanceId = false
        };

        Assert.False(args.IncludeActivityInstanceId);
    }

    [Fact]
    public async Task CreateBookmark_StimulusConvenience_MatchesParameterlessIdentityInclusion()
    {
        var context = await CreateContextAsync();

        var noArgs = context.CreateBookmark();
        var withStimulus = context.CreateBookmark("order-created");

        Assert.Contains(context.Id, noArgs.Hash);
        Assert.Contains(context.Id, withStimulus.Hash);
    }

    [Fact]
    public async Task CreateBookmark_UnsetArgs_MatchesStimulusConvenienceHash()
    {
        var context = await CreateContextAsync();
        const string stimulus = "order-created";

        var fromConvenience = context.CreateBookmark(stimulus);
        var fromUnsetArgs = context.CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = stimulus
        });
        var fromCallbackOverload = context.CreateBookmark(stimulus, callback: null);

        Assert.Equal(fromConvenience.Hash, fromUnsetArgs.Hash);
        Assert.Equal(fromConvenience.Hash, fromCallbackOverload.Hash);
        Assert.Contains(context.Id, fromUnsetArgs.Hash);
    }

    [Fact]
    public async Task CreateBookmark_ExplicitFalse_ExcludesActivityInstanceIdFromHash()
    {
        var context = await CreateContextAsync();
        const string stimulus = "order-created";

        var included = context.CreateBookmark(stimulus);
        var excluded = context.CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = stimulus,
            IncludeActivityInstanceId = false
        });

        Assert.Contains(context.Id, included.Hash);
        Assert.DoesNotContain(context.Id, excluded.Hash);
        Assert.NotEqual(included.Hash, excluded.Hash);
    }

    private static Task<ActivityExecutionContext> CreateContextAsync()
    {
        var fixture = new ActivityTestFixture(new WriteLine("test"));
        fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IHasher, ConcatHasher>();
            services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>();
        });
        return fixture.BuildAsync();
    }

    /// <summary>
    /// Encodes hash inputs so tests can see whether the activity instance ID was passed through.
    /// </summary>
    private sealed class ConcatHasher : IHasher
    {
        public string Hash(string value) => value;

        public string Hash(params object?[] values) =>
            string.Join("|", values.Select(value => value?.ToString() ?? string.Empty));
    }
}

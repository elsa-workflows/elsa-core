using Elsa.DevOps.GitHub.Activities.Comments;
using Elsa.DevOps.GitHub.Activities.Gists;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DevOps.GitHub.UnitTests;

/// <summary>
/// Each V2 activity nested in a Sequence keeps its own workflow activity ID next to its provider input.
/// </summary>
public sealed class GitHubNestedActivityIdTests
{
    [Fact]
    public async Task NestedV2ActivitiesKeepTheirWorkflowIdsThroughBothSerializerEntryPoints()
    {
        var services = new ServiceCollection();
        services.AddElsa();
        services.AddActivitiesFrom(typeof(GetCommentV2).Assembly);
        services.AddActivity<Sequence>();
        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IActivityRegistryPopulator>().PopulateRegistryAsync(CancellationToken.None);
        var serializer = provider.GetRequiredService<IActivitySerializer>();

        IActivity[] children =
        [
            new GetCommentV2 { Id = "nested-get-comment", Owner = new("owner"), Repository = new("repository"), CommentId = new(42) },
            new DeleteCommentV2 { Id = "nested-delete-comment", Owner = new("owner"), Repository = new("repository"), CommentId = new(43) },
            new UpdateCommentV2 { Id = "nested-update-comment", Owner = new("owner"), Repository = new("repository"), CommentId = new(44), Body = new("body") },
            new GetGistV2 { Id = "nested-get-gist", GistId = new("gist-45") }
        ];
        var sequence = new Sequence { Id = "parent-sequence", Activities = children };

        foreach (var json in new[] { serializer.Serialize((IActivity)sequence), serializer.Serialize((object)sequence) })
        {
            var restored = Assert.IsType<Sequence>(serializer.Deserialize(json));
            Assert.Equal("parent-sequence", restored.Id);
            Assert.Equal(children.Select(child => child.Id), restored.Activities.Select(child => child.Id));
        }
    }
}

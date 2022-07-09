using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.Triggers;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Moq;
using Xunit;

namespace Elsa.Triggers
{
    public class TriggersForActivityBlueprintAndWorkflowProviderTests
    {
        [Theory(DisplayName = "The GetTriggersForActivityBlueprintAsync returns a trigger for every bookmark in the bookmark providers that support the activity"), AutoMoqData]
        public async Task GetTriggersForActivityBlueprintAsyncReturnsTriggersForEachBookmarkInSupportedBookmarkProviders(IBookmarkHasher bookmarkHasher,
            IBookmarkProvider bookmarkProvider1,
            IBookmarkProvider unsupportedBookmarkProvider,
            IBookmarkProvider bookmarkProvider2,
            ICreatesActivityExecutionContextForActivityBlueprint activityExecutionContextFactory,
            [Frozen] IActivityBlueprint activityBlueprint,
            [WithAutofixtureResolution, Frozen] IServiceProvider serviceProvider,
            [Frozen] IWorkflowBlueprint workflowBlueprint,
            [OmitOnRecursion, Frozen] WorkflowInstance workflowInstance,
            [Frozen] WorkflowExecutionContext workflowExecutionContext,
            [NoAutoProperties] ActivityExecutionContext activityExecutionContext,
            ActivityType activityType,
            IBookmark bookmark1,
            IBookmark bookmark2,
            IBookmark bookmark3,
            IBookmark bookmark4)
        {
            var sut = new TriggersForActivityBlueprintAndWorkflowProvider(bookmarkHasher,
                new[] { bookmarkProvider1, unsupportedBookmarkProvider, bookmarkProvider2 },
                activityExecutionContextFactory);

            Mock.Get(activityExecutionContextFactory)
                .Setup(x => x.CreateActivityExecutionContext(activityBlueprint, workflowExecutionContext, default))
                .Returns(activityExecutionContext);
            Mock.Get(activityBlueprint).SetupGet(x => x.Type).Returns(activityType.TypeName);
            Mock.Get(bookmarkProvider1)
                .Setup(x => x.SupportsActivityAsync(It.Is<BookmarkProviderContext>(c => c.ActivityType == activityType), default))
                .Returns(() => ValueTask.FromResult(true));
            Mock.Get(unsupportedBookmarkProvider)
                .Setup(x => x.SupportsActivityAsync(It.Is<BookmarkProviderContext>(c => c.ActivityType == activityType), default))
                .Returns(() => ValueTask.FromResult(false));
            Mock.Get(bookmarkProvider2)
                .Setup(x => x.SupportsActivityAsync(It.Is<BookmarkProviderContext>(c => c.ActivityType == activityType), default))
                .Returns(() => ValueTask.FromResult(true));
            Mock.Get(bookmarkProvider1)
                .Setup(x => x.GetBookmarksAsync(It.IsAny<BookmarkProviderContext>(), default))
                .Returns(() => ValueTask.FromResult<IEnumerable<BookmarkResult>>(new[] { new BookmarkResult(bookmark1), new BookmarkResult(bookmark2) }));
            Mock.Get(bookmarkProvider2)
                .Setup(x => x.GetBookmarksAsync(It.IsAny<BookmarkProviderContext>(), default))
                .Returns(() => ValueTask.FromResult<IEnumerable<BookmarkResult>>(new[] { new BookmarkResult(bookmark3), new BookmarkResult(bookmark4) }));

            var result = await sut.GetTriggersForActivityBlueprintAsync(activityBlueprint,
                workflowExecutionContext,
                new Dictionary<string, ActivityType> { { activityType.TypeName, activityType } });

            Assert.True(result.Any(x => x.Bookmark == bookmark1), "Result contains a trigger for bookmark 1");
            Assert.True(result.Any(x => x.Bookmark == bookmark2), "Result contains a trigger for bookmark 2");
            Assert.True(result.Any(x => x.Bookmark == bookmark3), "Result contains a trigger for bookmark 3");
            Assert.True(result.Any(x => x.Bookmark == bookmark4), "Result contains a trigger for bookmark 4");
            Assert.True(result.Count() == 4, "Result has 4 items");
        }
    }
}
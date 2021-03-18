using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Elsa.ActivityProviders;
using Elsa.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Triggers;
using Moq;
using Xunit;

namespace Elsa.UnitTests.Triggers
{
    public class TriggersForBlueprintsProviderTests
    {
        [Theory(DisplayName = "The GetTriggersAsync method should return all triggers for the compatible bookmarks of the start activities of the blueprints.  This is actually an integration test."), AutoMoqData]
        public async Task GetTriggersAsyncGetsAllTriggersForAllBlueprintsStartActivitiesAndCompatibleBookmarks(IActivityTypeService activityTypeService,
                                                                                                               IBookmarkHasher bookmarkHasher,
                                                                                                               IBookmarkProvider provider1,
                                                                                                               IBookmarkProvider provider2,
                                                                                                               IBookmarkProvider provider3,
                                                                                                               [AutofixtureServiceProvider] IServiceProvider serviceProvider,
                                                                                                               IWorkflowFactory workflowFactory,
                                                                                                               ActivityType activityType1,
                                                                                                               ActivityType activityType2,
                                                                                                               ActivityType activityType3,
                                                                                                               IWorkflowBlueprint workflowBlueprint1,
                                                                                                               IWorkflowBlueprint workflowBlueprint2,
                                                                                                               IActivityBlueprint activityBlueprint1,
                                                                                                               IActivityBlueprint activityBlueprint2,
                                                                                                               IActivityBlueprint activityBlueprint3,
                                                                                                               IActivityBlueprint activityBlueprint4,
                                                                                                               IActivityBlueprint activityBlueprint5,
                                                                                                               IActivityBlueprint activityBlueprint6,
                                                                                                               string activityBlueprintId1,
                                                                                                               string activityBlueprintId2,
                                                                                                               string activityBlueprintId3,
                                                                                                               string activityBlueprintId4,
                                                                                                               string activityBlueprintId5,
                                                                                                               string activityBlueprintId6,
                                                                                                               IConnection connection1,
                                                                                                               IConnection connection2,
                                                                                                               IConnection connection3,
                                                                                                               ITargetEndpoint endpoint1,
                                                                                                               ITargetEndpoint endpoint2,
                                                                                                               ITargetEndpoint endpoint3,
                                                                                                               [OmitOnRecursion] WorkflowInstance workflow1,
                                                                                                               [OmitOnRecursion] WorkflowInstance workflow2,
                                                                                                               IBookmark bookmark1,
                                                                                                               IBookmark bookmark2,
                                                                                                               IBookmark bookmark3,
                                                                                                               IBookmark bookmark4,
                                                                                                               IBookmark bookmark5,
                                                                                                               IBookmark bookmark6,
                                                                                                               string bookmarkHash1,
                                                                                                               string bookmarkHash2,
                                                                                                               string bookmarkHash3,
                                                                                                               string bookmarkHash4,
                                                                                                               string bookmarkHash5,
                                                                                                               string bookmarkHash6)
        {
            var workflowExecutionContextFactory = new WorkflowExecutionContextForWorkflowBlueprintFactory(serviceProvider, workflowFactory);
            var activityExecutionContextFactory = new ActivityExecutionContextForActivityBlueprintFactory(serviceProvider);
            var triggersForActivityProvider = new TriggersForActivityBlueprintAndWorkflowProvider(bookmarkHasher,
                                                                                                  new[] { provider1, provider2, provider3 },
                                                                                                  activityExecutionContextFactory);
            var startingActivitiesProvider = new StartActivitiesForCompositeActivityBlueprintProvider();

            var sut = new TriggersForBlueprintsProvider(activityTypeService,
                                                        workflowExecutionContextFactory,
                                                        triggersForActivityProvider,
                                                        startingActivitiesProvider);

            Mock.Get(activityTypeService)
                .Setup(x => x.GetActivityTypesAsync(default))
                .Returns(ValueTask.FromResult<IEnumerable<ActivityType>>(new [] { activityType1, activityType2, activityType3 }));
            Mock.Get(workflowBlueprint1).SetupGet(x => x.Connections).Returns(new [] { connection1 });
            Mock.Get(workflowBlueprint2).SetupGet(x => x.Connections).Returns(new [] { connection2, connection3 });
            Mock.Get(workflowBlueprint1).SetupGet(x => x.Activities).Returns(new [] { activityBlueprint1, activityBlueprint2, activityBlueprint4 });
            Mock.Get(workflowBlueprint2).SetupGet(x => x.Activities).Returns(new [] { activityBlueprint3, activityBlueprint5, activityBlueprint6 });
            Mock.Get(workflowFactory)
                .Setup(x => x.InstantiateAsync(workflowBlueprint1, null, null, default))
                .Returns(Task.FromResult(workflow1));
            Mock.Get(workflowFactory)
                .Setup(x => x.InstantiateAsync(workflowBlueprint2, null, null, default))
                .Returns(Task.FromResult(workflow2));
            Mock.Get(connection1).SetupGet(x => x.Target).Returns(endpoint1);
            Mock.Get(connection2).SetupGet(x => x.Target).Returns(endpoint2);
            Mock.Get(connection3).SetupGet(x => x.Target).Returns(endpoint3);
            Mock.Get(endpoint1).SetupGet(x => x.Activity).Returns(activityBlueprint4);
            Mock.Get(endpoint2).SetupGet(x => x.Activity).Returns(activityBlueprint5);
            Mock.Get(endpoint3).SetupGet(x => x.Activity).Returns(activityBlueprint6);
            Mock.Get(activityBlueprint1).SetupGet(x => x.Type).Returns(activityType1.TypeName);
            Mock.Get(activityBlueprint2).SetupGet(x => x.Type).Returns(activityType2.TypeName);
            Mock.Get(activityBlueprint3).SetupGet(x => x.Type).Returns(activityType3.TypeName);
            Mock.Get(activityBlueprint1).SetupGet(x => x.Id).Returns(activityBlueprintId1);
            Mock.Get(activityBlueprint2).SetupGet(x => x.Id).Returns(activityBlueprintId2);
            Mock.Get(activityBlueprint3).SetupGet(x => x.Id).Returns(activityBlueprintId3);
            Mock.Get(activityBlueprint4).SetupGet(x => x.Id).Returns(activityBlueprintId4);
            Mock.Get(activityBlueprint5).SetupGet(x => x.Id).Returns(activityBlueprintId5);
            Mock.Get(activityBlueprint6).SetupGet(x => x.Id).Returns(activityBlueprintId6);
            SetupProviderToBeCompatibleWithActivityType(provider1, activityType1);
            SetupProviderToBeCompatibleWithActivityType(provider2, activityType2);
            SetupProviderToBeCompatibleWithActivityType(provider3, activityType3);
            Mock.Get(provider1)
                .Setup(x => x.GetBookmarksAsync(It.IsAny<BookmarkProviderContext>(), default))
                .Returns(ValueTask.FromResult<IEnumerable<IBookmark>>(new [] { bookmark1, bookmark2 }));
            Mock.Get(provider2)
                .Setup(x => x.GetBookmarksAsync(It.IsAny<BookmarkProviderContext>(), default))
                .Returns(ValueTask.FromResult<IEnumerable<IBookmark>>(new [] { bookmark3, bookmark4 }));
            Mock.Get(provider3)
                .Setup(x => x.GetBookmarksAsync(It.IsAny<BookmarkProviderContext>(), default))
                .Returns(ValueTask.FromResult<IEnumerable<IBookmark>>(new [] { bookmark5, bookmark6 }));
            Mock.Get(bookmarkHasher).Setup(x => x.Hash(bookmark1)).Returns(bookmarkHash1);
            Mock.Get(bookmarkHasher).Setup(x => x.Hash(bookmark2)).Returns(bookmarkHash2);
            Mock.Get(bookmarkHasher).Setup(x => x.Hash(bookmark3)).Returns(bookmarkHash3);
            Mock.Get(bookmarkHasher).Setup(x => x.Hash(bookmark4)).Returns(bookmarkHash4);
            Mock.Get(bookmarkHasher).Setup(x => x.Hash(bookmark5)).Returns(bookmarkHash5);
            Mock.Get(bookmarkHasher).Setup(x => x.Hash(bookmark6)).Returns(bookmarkHash6);

            var result = await sut.GetTriggersAsync(new [] { workflowBlueprint1, workflowBlueprint2 });

            Assert.Equal(6, result.Count());
            Assert.True(ContainsTriggerWithHash(result, bookmarkHash1)
                        && ContainsTriggerWithHash(result, bookmarkHash2)
                        && ContainsTriggerWithHash(result, bookmarkHash3)
                        && ContainsTriggerWithHash(result, bookmarkHash4)
                        && ContainsTriggerWithHash(result, bookmarkHash5)
                        && ContainsTriggerWithHash(result, bookmarkHash6),
                        "The returned triggers should contain a trigger with every one of the six expected hashes");
        }

        void SetupProviderToBeCompatibleWithActivityType(IBookmarkProvider provider, ActivityType activityType)
        {
            Mock.Get(provider)
                .Setup(x => x.SupportsActivityAsync(It.IsAny<BookmarkProviderContext>(), default))
                .Returns(ValueTask.FromResult(false));
            Mock.Get(provider)
                .Setup(x => x.SupportsActivityAsync(It.Is<BookmarkProviderContext>(c => c.ActivityType == activityType), default))
                .Returns(ValueTask.FromResult(true));
        }

        bool ContainsTriggerWithHash(IEnumerable<WorkflowTrigger> triggers, string hash)
            => triggers.Any(x => Equals(x.BookmarkHash, hash));
    }
}
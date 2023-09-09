using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Extensions;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.Bookmarks;
using Elsa.Persistence.Specifications.Triggers;
using Elsa.Services;
using Rebus.Extensions;
using Rebus.Handlers;

namespace Elsa.Activities.Http.Consumers
{
    public class UpdateRouteTable :
        IHandleMessages<TriggerIndexingFinished>,
        IHandleMessages<TriggersDeleted>,
        IHandleMessages<BookmarkIndexingFinished>,
        IHandleMessages<BookmarksDeleted>
    {
        private readonly IRouteTable _routeTable;
        private readonly ITriggerStore _triggerStore;
        private readonly IBookmarkStore _bookmarkStore;
        private readonly IBookmarkHasher _bookmarkHasher;
        private readonly IBookmarkSerializer _bookmarkSerializer;

        public UpdateRouteTable(
            IRouteTable routeTable,
            ITriggerStore triggerStore,
            IBookmarkStore bookmarkStore,
            IBookmarkHasher bookmarkHasher,
            IBookmarkSerializer bookmarkSerializer)
        {
            _routeTable = routeTable;
            _triggerStore = triggerStore;
            _bookmarkStore = bookmarkStore;
            _bookmarkHasher = bookmarkHasher;
            _bookmarkSerializer = bookmarkSerializer;
        }

        public Task Handle(TriggerIndexingFinished message)
        {
            _routeTable.AddRoutes(message.Triggers);
            return Task.CompletedTask;
        }

        public async Task Handle(TriggersDeleted message)
        {
            var specification = new WorkflowDefinitionIdSpecification(message.WorkflowDefinitionId);
            var currentDefinitionTriggers = (await _triggerStore.FindManyAsync(specification)).ToList();
            var triggers = message.Triggers.Where(x => !Exists(x)).ToList();

            _routeTable.RemoveRoutes(triggers);
            return;

            bool Exists(Trigger trigger) => currentDefinitionTriggers.Any(
                x => x.ActivityId == trigger.ActivityId
                     && x.WorkflowDefinitionId == trigger.WorkflowDefinitionId
                     && x.ModelType == typeof(HttpEndpointBookmark).GetSimpleAssemblyQualifiedName()
                     && trigger.ModelType == typeof(HttpEndpointBookmark).GetSimpleAssemblyQualifiedName()
                     && _bookmarkHasher.Hash(Deserialize(x)) == _bookmarkHasher.Hash(Deserialize(trigger)));
        }

        public Task Handle(BookmarkIndexingFinished message)
        {
            _routeTable.AddRoutes(message.Bookmarks);
            return Task.CompletedTask;
        }

        public async Task Handle(BookmarksDeleted message)
        {
            var specification = new WorkflowInstanceIdSpecification(message.WorkflowInstanceId);
            var currentInstanceBookmarks = (await _bookmarkStore.FindManyAsync(specification)).ToList();
            var bookmarks = message.Bookmarks.Where(x => !Exists(x)).ToList();
            
            _routeTable.RemoveRoutes(bookmarks);
            return;

            bool Exists(Bookmark bookmark) => currentInstanceBookmarks.Any(x =>
                x.ActivityId == bookmark.ActivityId
                && x.WorkflowInstanceId == bookmark.WorkflowInstanceId
                && x.ModelType == typeof(HttpEndpointBookmark).GetSimpleAssemblyQualifiedName()
                && bookmark.ModelType == typeof(HttpEndpointBookmark).GetSimpleAssemblyQualifiedName()
                && _bookmarkHasher.Hash(Deserialize(x)) == _bookmarkHasher.Hash(Deserialize(bookmark)));
        }

        private HttpEndpointBookmark Deserialize(Trigger trigger) => Deserialize(trigger.Model);
        private HttpEndpointBookmark Deserialize(Bookmark bookmark) => Deserialize(bookmark.Model);
        private HttpEndpointBookmark Deserialize(string model) => _bookmarkSerializer.Deserialize<HttpEndpointBookmark>(model);
    }
}
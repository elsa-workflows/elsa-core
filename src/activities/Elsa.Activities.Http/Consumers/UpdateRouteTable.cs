using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Extensions;
using Elsa.Events;
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
        public UpdateRouteTable(IRouteTable routeTable) => _routeTable = routeTable;

        public Task Handle(TriggerIndexingFinished message)
        {
            _routeTable.AddRoutes(message.Triggers);
            return Task.CompletedTask;
        }

        public Task Handle(TriggersDeleted message)
        {
            _routeTable.RemoveRoutes(message.Triggers);
            return Task.CompletedTask;
        }

        public Task Handle(BookmarkIndexingFinished message)
        {
            _routeTable.AddRoutes(message.Bookmarks);
            return Task.CompletedTask;
        }

        public Task Handle(BookmarksDeleted message)
        {
            _routeTable.RemoveRoutes (message.Bookmarks);
            return Task.CompletedTask;
        }
    }
}
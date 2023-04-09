using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Models.Notifications;

public record IndexedWorkflowBookmarks(string InstanceId, ICollection<Bookmark> AddedBookmarks, ICollection<Bookmark> RemovedBookmarks, ICollection<Bookmark> UnchangedBookmarks);
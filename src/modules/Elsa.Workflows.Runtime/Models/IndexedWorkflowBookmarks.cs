using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Runtime.Models;

public record IndexedWorkflowBookmarks(string InstanceId, ICollection<Bookmark> AddedBookmarks, ICollection<Bookmark> RemovedBookmarks, ICollection<Bookmark> UnchangedBookmarks);
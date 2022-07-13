using System.Collections.Generic;
using Elsa.Models;
using MediatR;

namespace Elsa.Events;

public record BookmarkIndexingFinished(string WorkflowInstanceId, IReadOnlyCollection<Bookmark> Bookmarks) : INotification;
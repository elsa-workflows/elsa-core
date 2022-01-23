using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;
using MediatR;

namespace Elsa.Events;

public record TriggersDeleted(IReadOnlyCollection<Trigger> Triggers) : INotification;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

internal record SignalHandlerRegistration(Type SignalType, Func<object, SignalContext, ValueTask> Handler);
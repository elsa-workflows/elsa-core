using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

internal record SignalHandlerRegistration(Type SignalType, Func<object, SignalContext, ValueTask> Handler);
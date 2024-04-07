using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.State;

namespace Elsa.ProtoActor.Mappers;

internal class ExceptionMapper
{
    public ExceptionState Map(ProtoExceptionState exception)
    {
        return new ExceptionState(Type.GetType(exception.Type)!, exception.Message, exception.StackTrace, exception.InnerException != null ? Map(exception.InnerException) : null);
    }

    public ProtoExceptionState Map(ExceptionState exception) =>
        new()
        {
            Type = exception.Type.AssemblyQualifiedName,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException != null ? Map(exception.InnerException) : null
        };
}
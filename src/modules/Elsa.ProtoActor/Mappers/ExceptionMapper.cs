using Elsa.Workflows.Core.State;
using ProtoException = Elsa.ProtoActor.Protos.ExceptionState;

namespace Elsa.ProtoActor.Mappers;

public class ExceptionMapper
{
    public ExceptionState Map(ProtoException exception) =>
        new(Type.GetType(exception.Type)!, exception.Message, exception.StackTrace, exception.InnerException != null ? Map(exception.InnerException) : null);
    
    public ProtoException Map(ExceptionState exception) =>
        new()
        {
            Type = exception.Type.AssemblyQualifiedName,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException != null ? Map(exception.InnerException) : null
        };
}
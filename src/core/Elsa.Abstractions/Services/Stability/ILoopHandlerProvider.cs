using System;

namespace Elsa.Services.Stability
{
    public interface ILoopHandlerProvider
    {
        ILoopHandler GetHandler(Type type);
    }
}
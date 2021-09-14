using System;

namespace Elsa.Services.Stability
{
    public interface ILoopDetectorProvider
    {
        ILoopDetector GetDetector(Type type);
    }
}
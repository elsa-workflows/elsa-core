using Microsoft.IO;

namespace Elsa.Helpers;

public static class StreamHelpers
{
    private static RecyclableMemoryStreamManager? _recyclableMemoryStreamManager;

    public static RecyclableMemoryStreamManager RecyclableMemoryStreamManager
    {
        get => _recyclableMemoryStreamManager ?? new(new RecyclableMemoryStreamManager.Options()
        {
            BlockSize = RecyclableMemoryStreamManager.DefaultBlockSize,
            LargeBufferMultiple = RecyclableMemoryStreamManager.DefaultLargeBufferMultiple,
            MaximumBufferSize = RecyclableMemoryStreamManager.DefaultMaximumBufferSize,
            GenerateCallStacks = false,
            AggressiveBufferReturn = false,
            MaximumLargePoolFreeBytes = 16 * 1024 * 1024 * 4,
            MaximumSmallPoolFreeBytes = 100 * 1024
        });

        set
        {
            if (_recyclableMemoryStreamManager is not null)
            {
                return;
            }
            
            _recyclableMemoryStreamManager = value;
        }
    }
}

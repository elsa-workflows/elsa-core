namespace Elsa.IO.Common;

/// <summary>
/// IO module constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Priorities for content resolver strategies.
    /// </summary>
    public static class StrategyPriorities
    {
        /// <summary>
        /// Stream content priority.
        /// </summary>
        public const float Stream = 0.0f;
        
        /// <summary>
        /// Byte array content priority.
        /// </summary>
        public const float ByteArray = 1.0f;
        
        /// <summary>
        /// Base64 content priority.
        /// </summary>
        public const float Base64 = 2.0f;
        
        /// <summary>
        /// File path content priority.
        /// </summary>
        public const float FilePath = 3.0f;
        
        /// <summary>
        /// Text content priority.
        /// </summary>
        public const float Text = 100.0f;
    }
}

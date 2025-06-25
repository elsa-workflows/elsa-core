namespace Elsa.IO.Common;

public static class Constants
{
    public static class StrategyPriorities
    {
        public const float Stream = 0.0f;
        public const float ByteArray = 1.0f;
        public const float Base64 = 2.0f;
        public const float Uri = 3.0f;
        public const float FilePath = 4.0f;
        public const float Text = 5.0f;
    }
}
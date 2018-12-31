using System.IO;
using System.Text;

namespace Elsa.Serialization.Extensions
{
    public static class StringExtensions
    {
        public static Stream ToStream(this string text)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static byte[] ToBytes(this string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }
    }
}
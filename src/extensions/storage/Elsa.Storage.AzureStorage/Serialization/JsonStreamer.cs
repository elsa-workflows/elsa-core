using System.IO.Pipelines;
using System.Text.Json;

namespace Elsa.Storage.AzureStorage.Serialization;

/// <summary>
/// A helper that serializes objects to JSON and streams the output in segments.
/// </summary>
public static class JsonStreamer
{
    /// <summary>
    /// Serializes the given object to JSON and streams the output in segments
    /// to the specified callback.
    /// </summary>
    /// <param name="value">The object to serialize to JSON.</param>
    /// <param name="processSegment">A callback that receives each chunk of serialized data as a byte array.</param>
    /// <param name="options">Optional JsonSerializerOptions to configure the serialization.</param>
    public static async Task SerializeAndStreamAsync(
        object value,
        Func<byte[], Task> processSegment,
        JsonSerializerOptions? options = null)
    {
        var pipe = new Pipe();
        var writerTask = WriteObjectAsync(value, pipe.Writer, options);
        var readerTask = ReadPipeAsync(pipe.Reader, processSegment);

        await Task.WhenAll(writerTask, readerTask);
    }

    private static async Task WriteObjectAsync(object value, PipeWriter writer, JsonSerializerOptions? options)
    {
        await JsonSerializer.SerializeAsync(writer.AsStream(), value, value.GetType(), options);
        await writer.CompleteAsync();
    }

    private static async Task ReadPipeAsync(PipeReader reader, Func<byte[], Task> onSegment)
    {
        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            if (buffer.IsEmpty && result.IsCompleted)
                break;
            
            foreach (var segment in buffer)
            {
                var chunk = segment.ToArray();
                await onSegment(chunk);
            }

            reader.AdvanceTo(buffer.End);
        }
        
        await reader.CompleteAsync();
    }
}
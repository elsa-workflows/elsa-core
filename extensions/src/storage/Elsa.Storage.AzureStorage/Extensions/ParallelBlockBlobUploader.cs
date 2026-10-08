using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Elsa.Storage.AzureStorage.Serialization;
using MemoryStream = System.IO.MemoryStream;

namespace Elsa.Storage.AzureStorage.Extensions;

/// <summary>
/// Provides methods for uploading large JSON data as blocks to Azure Blob Storage
/// with support for parallelism and customizable block sizes.
/// </summary>
public static class ParallelBlockBlobUploader
{
    /// <summary>
    /// Streams the given value as JSON to Azure Blob Storage in blocks,
    /// allowing parallel uploads and reliable handling of large data.
    /// </summary>
    /// <param name="blobServiceClient">The BlobServiceClient to use.</param>
    /// <param name="containerName">Name of the target container.</param>
    /// <param name="blobName">Name of the blob to create/overwrite.</param>
    /// <param name="value">The object to serialize and upload.</param>
    /// <param name="blockSizeInBytes">
    /// The approximate size of each block. Default = 4 MB.
    /// </param>
    /// <param name="maxConcurrency">
    /// The maximum number of blocks to upload in parallel.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task UploadJsonAsBlocksAsync(this BlobServiceClient blobServiceClient,
        string containerName,
        string blobName,
        object value,
        int blockSizeInBytes = 4 * 1024 * 1024,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.UploadJsonAsBlocksAsync(blobName, value, blockSizeInBytes, maxConcurrency, cancellationToken);
    }
    
    /// <summary>
    /// Streams the given value as JSON to Azure Blob Storage in blocks,
    /// allowing parallel uploads and reliable handling of large data.
    /// </summary>
    /// <param name="containerClient">The BlobContainerClient to use.</param>
    /// <param name="blobName">Name of the blob to create/overwrite.</param>
    /// <param name="value">The object to serialize and upload.</param>
    /// <param name="blockSizeInBytes">
    /// The approximate size of each block. Default = 4 MB.
    /// </param>
    /// <param name="maxConcurrency">
    /// The maximum number of blocks to upload in parallel.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task UploadJsonAsBlocksAsync(this BlobContainerClient containerClient,
        string blobName,
        object value,
        int blockSizeInBytes = 4 * 1024 * 1024,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        var blockBlobClient = containerClient.GetBlockBlobClient(blobName);
        await blockBlobClient.UploadJsonAsBlocksAsync(value, blockSizeInBytes, maxConcurrency, cancellationToken);
    }

    /// <summary>
    /// Streams the given value as JSON to Azure Blob Storage in blocks,
    /// allowing parallel uploads and reliable handling of large data.
    /// </summary>
    /// <param name="blockBlobClient">The BlockBlobClient to use.</param>
    /// <param name="value">The object to serialize and upload.</param>
    /// <param name="blockSizeInBytes">
    /// The approximate size of each block. Default = 4 MB.
    /// </param>
    /// <param name="maxConcurrency">
    /// The maximum number of blocks to upload in parallel.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task UploadJsonAsBlocksAsync(this BlockBlobClient blockBlobClient,
        object value,
        int blockSizeInBytes = 4 * 1024 * 1024,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        // List to keep track of block IDs in upload order.
        var blockIds = new List<string>();
        
        // We'll accumulate chunk data in a MemoryStream until we exceed blockSizeInBytes
        var bufferStream = new MemoryStream();
        var blockIndex = 0;

        // Maintain a list of tasks for parallel block staging.
        var ongoingTasks = new List<Task>();

        // Read from JsonStreamer in segments.
        await JsonStreamer.SerializeAndStreamAsync(value, async segment =>
        {
            // If adding this segment exceeds the block size, flush the current buffer first.
            if (bufferStream.Length + segment.Length > blockSizeInBytes)
                await FlushBufferAsync();

            // Write a new segment into the buffer.
            await bufferStream.WriteAsync(segment, cancellationToken);
        });

        // Flush any final partial block
        await FlushBufferAsync();

        // Wait for the remaining block uploads to finish
        await Task.WhenAll(ongoingTasks);

        // Commit the block list in order
        await blockBlobClient.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);
        return;

        // Action to stage a block to Azure
        async Task StageBlockAsync(byte[] bytes)
        {
            var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"block-{blockIndex++:000000}"));
            blockIds.Add(blockId);

            using var chunkStream = new MemoryStream(bytes);
            await blockBlobClient.StageBlockAsync(blockId, chunkStream, cancellationToken: cancellationToken);
        }

        // Helper to flush the buffer to a block.
        async Task FlushBufferAsync()
        {
            // If we have data in the buffer, copy it out and stage it.
            if (bufferStream.Length > 0)
            {
                var stagedBytes = bufferStream.ToArray();
                bufferStream.SetLength(0);

                // Start staging a new block.
                var uploadTask = StageBlockAsync(stagedBytes);
                ongoingTasks.Add(uploadTask);

                // If we reached max concurrency, wait for at least one upload to finish.
                if (ongoingTasks.Count >= maxConcurrency)
                {
                    var finished = await Task.WhenAny(ongoingTasks);
                    ongoingTasks.Remove(finished);
                    await finished; // Observe any exceptions.
                }
            }
        }
    }
}
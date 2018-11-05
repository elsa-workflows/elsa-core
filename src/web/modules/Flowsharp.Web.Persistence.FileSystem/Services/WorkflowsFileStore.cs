using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OrchardCore.FileStorage;

namespace Flowsharp.Web.Persistence.FileSystem.Services
{
    public class WorkflowsFileStore : IWorkflowsFileStore
    {
        private readonly IFileStore fileStore;

        public WorkflowsFileStore(IFileStore fileStore)
        {
            this.fileStore = fileStore;
        }

        public Task<IFileStoreEntry> GetFileInfoAsync(string path) => fileStore.GetFileInfoAsync(path);
        public Task<IFileStoreEntry> GetDirectoryInfoAsync(string path) => fileStore.GetDirectoryInfoAsync(path);
        public Task<IEnumerable<IFileStoreEntry>> GetDirectoryContentAsync(string path = null, bool includeSubDirectories = false) => fileStore.GetDirectoryContentAsync(path, includeSubDirectories);
        public Task<bool> TryCreateDirectoryAsync(string path) => fileStore.TryCreateDirectoryAsync(path);
        public Task<bool> TryDeleteFileAsync(string path) => fileStore.TryDeleteFileAsync(path);
        public Task<bool> TryDeleteDirectoryAsync(string path) => fileStore.TryDeleteDirectoryAsync(path);
        public Task MoveFileAsync(string oldPath, string newPath) => fileStore.MoveFileAsync(oldPath, newPath);
        public Task CopyFileAsync(string srcPath, string dstPath) => fileStore.CopyFileAsync(srcPath, dstPath);
        public Task<Stream> GetFileStreamAsync(string path) => fileStore.GetFileStreamAsync(path);
        public Task CreateFileFromStream(string path, Stream inputStream, bool overwrite = false) => fileStore.CreateFileFromStream(path, inputStream, overwrite);
    }
}
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.FileSystem.Options;
using Elsa.Serialization;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.FileSystem
{
    public class FileSystemWorkflowProvider : IFileSystemWorkflowProvider
    {
        private readonly IFileSystem fileSystem;
        private readonly IWorkflowSerializer workflowSerializer;
        private readonly string rootDirectory;
        private readonly string format;

        public FileSystemWorkflowProvider(
            IOptions<FileSystemStoreOptions> options,
            IFileSystem fileSystem,
            IWorkflowSerializer workflowSerializer)
        {
            this.fileSystem = fileSystem;
            this.workflowSerializer = workflowSerializer;
            rootDirectory = options.Value.Directory;
            format = options.Value.Format;
        }

        public async Task SaveAsync(string directory, Workflow value, CancellationToken cancellationToken)
        {
            var data = await workflowSerializer.SerializeAsync(value, format, cancellationToken);
            var fileName = Path.HasExtension(value.Id) ? value.Id : $"{value.Id}.{format.ToLower()}";
            var path = fileSystem.Path.Combine(rootDirectory, directory, fileName);

            EnsureWorkflowsDirectory(path);
            fileSystem.File.WriteAllText(path, data);
        }

        public async Task<IEnumerable<Workflow>> ListAsync(string directory, CancellationToken cancellationToken)
        {
            var path = fileSystem.Path.Combine(rootDirectory, directory);
            EnsureWorkflowsDirectory(path);
            
            var files = fileSystem.Directory.GetFiles(path);
            var loadTasks = files.Select(x => LoadWorkflowAsync(x, cancellationToken));
            return await Task.WhenAll(loadTasks);
        }

        private async Task<Workflow> LoadWorkflowAsync(string path, CancellationToken cancellationToken)
        {
            var data = fileSystem.File.ReadAllText(path);
            return await workflowSerializer.DeserializeAsync(data, format, cancellationToken);
        }

        private void EnsureWorkflowsDirectory(string path)
        {
            fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
    }
}
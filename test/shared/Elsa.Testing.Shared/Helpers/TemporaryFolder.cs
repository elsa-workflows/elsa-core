using System;
using System.IO;

namespace Elsa.Testing.Shared.Helpers
{
    public class TemporaryFolder : IDisposable
    {
        private readonly bool _deleteOnDispose;

        public TemporaryFolder(bool deleteOnDispose = true)
        {
            Folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _deleteOnDispose = deleteOnDispose;
        }

        public string Folder { get; }

        /// <summary>
        /// Gets a path string for a file or folder which is contained within the <see cref="Folder"/>.
        /// </summary>
        /// <param name="relativePath">The relative path, within the temporary folder.</param>
        /// <returns>A path for content within the temporary folder.</returns>
        public string GetContainedPath(string relativePath)
        {
            Directory.CreateDirectory(Folder);
            return Path.Combine(Folder, relativePath);
        }

        public void Dispose()
        {
            if (_deleteOnDispose)
                if (Directory.Exists(Folder))
                    Directory.Delete(Folder, true);
        }

        public override string ToString() => Folder;
    }
}
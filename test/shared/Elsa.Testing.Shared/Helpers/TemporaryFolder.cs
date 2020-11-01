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

        public void Dispose()
        {
            if (_deleteOnDispose)
                if (Directory.Exists(Folder))
                    Directory.Delete(Folder, true);
        }

        public override string ToString() => Folder;
    }
}
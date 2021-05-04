using System.IO;
using System.Threading.Tasks;

namespace Elsa.ComponentTests.Helpers
{
    public static class AssetHelper
    {
        private static readonly string AssetsDirectory;
        static AssetHelper() => AssetsDirectory = Path.Combine(Path.GetDirectoryName(typeof(AssetHelper).Assembly.Location)!, "Assets");
        public static string GetAssetPath(string filename) => Path.Combine(AssetsDirectory, filename);
        public static Task<string> ReadAssetAsync(string filename) => File.ReadAllTextAsync(Path.Combine(AssetsDirectory, filename));
    }
}
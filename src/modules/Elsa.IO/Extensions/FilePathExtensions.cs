namespace Elsa.IO.Extensions;

public static class FilePathExtensions
{
    public static string CleanFilePath(this string filePath)
    {
        // Clean up the path - trim quotes and whitespace that might come from copy-paste
        filePath = filePath.Trim().Trim('"', '\'');

        // Replace backslashes with forward slashes on Unix/Mac systems
        if (Path.DirectorySeparatorChar == '/')
        {
            filePath = filePath.Replace('\\', '/');
        }

        return filePath;
    }
}
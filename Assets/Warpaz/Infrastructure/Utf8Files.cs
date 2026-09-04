using System.IO;
using System.Text;

namespace Warpaz.Infrastructure
{
    /// <summary>
    /// UTF-8 file helpers. Domain code must not call this.
    /// </summary>
    public static class Utf8Files
    {
        public static string ReadAll(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public static void WriteAll(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }
}

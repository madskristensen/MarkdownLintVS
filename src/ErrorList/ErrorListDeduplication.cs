using System.Collections.Generic;
using System.Linq;

namespace MarkdownLintVS.ErrorList
{
    internal static class ErrorListDeduplication
    {
        internal static bool ShouldIncludeFolderLintError(string filePath, IEnumerable<string> liveFilePaths)
        {
            return !liveFilePaths.Any(
                liveFilePath => string.Equals(liveFilePath, filePath, StringComparison.OrdinalIgnoreCase));
        }
    }
}

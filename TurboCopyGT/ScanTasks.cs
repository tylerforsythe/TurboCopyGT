using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.InteropServices;

namespace TurboCopyGT
{
    internal class ScanTasks
    {
        /// <summary>
        /// Found FileSystemName.MatchesSimpleExpression @ https://stackoverflow.com/a/66465594/7656
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool IsPathAtDiskRoot(string path) {
            if (path.StartsWith(@"\\") && path.Count(c => c == '\\') == 3)
                return true;
            if (FileSystemName.MatchesSimpleExpression(@"?:\", path) && path.Count(c => c == '\\') == 1)
                return true;
            return false;
        }

        public static void ScanForAllDirectoriesAndFiles(string path, IBaseActionDetails details) {
            // these are Windows-specific directories that appear at *disk root*.
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                var isDiskRoot = IsPathAtDiskRoot(path);
                if (isDiskRoot && (path.EndsWith("System Volume Information") ||
                                   path.EndsWith("$RECYCLE.BIN") ||
                                   path.EndsWith("Recovery")))
                    return;
            }

            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles()) {
                if (StringTasks.DoAnyPatternsMatch(details.ExcludedFilePatterns, file.FullName))
                    continue;
                details.Files.Add(file.FullName);
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                if (StringTasks.DoAnyPatternsMatch(details.ExcludedDirectoryPatterns, dir.FullName))
                    continue;
                details.Directories.Add(dir.FullName);
                ScanForAllDirectoriesAndFiles(dir.FullName, details);
            }
        }
    }
}

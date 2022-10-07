using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace TurboCopyGT
{
    internal static class ScanTasks
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
        
        public static void ShuffleFilesAndDirectories(IBaseActionDetails details) {
            details.Files.Shuffle();
            details.Directories.Shuffle();
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
        
        internal class ThreadSafeRandom
        {
            private static readonly Random _global = new Random();
            private static readonly ThreadLocal<Random> _local = new ThreadLocal<Random>(() =>
            {
                int seed;
                lock (_global)
                {
                    seed = _global.Next();
                }
                return new Random(seed);
            });

            public static int Next(int maxValue)
            {
                return _local.Value.Next(maxValue);
            }
        }
    }
}

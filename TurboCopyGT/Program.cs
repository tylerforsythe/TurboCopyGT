using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.InteropServices;
using Amib.Threading;

namespace TurboCopyGT
{
    class Program
    {
        private static SmartThreadPool _smartThreadPool;

        private static readonly int TOTAL_THREADS = (int)Math.Round(Environment.ProcessorCount * 2.6);

        static void Main(string[] args) {
            var testMode = false;
            //Console.WriteLine("Hello World!");
            _smartThreadPool = new SmartThreadPool();
            _smartThreadPool.MaxThreads = TOTAL_THREADS;

            var details = new CopyDetails();
            if (args != null && args.Length == 2) {
                testMode = false;
                details.SourcePath = args[0];
                details.DestPath = args[1];
            }
            else {
                Console.WriteLine("NO ARGS -- TEST MODE");
                testMode = true;
                details.SourcePath = @"C:\temp\mblf-node-modules-20210625";
                details.DestPath = @"H:\temp\mbl_build\copy-test-destination";
            }

            // figure out to read arguments in a new way so that we can do things like -xf and -xd for excluded files & directories [list]
            // when reading the X-files and dirs lists, if each individual entry does not contain a * or ?, then add * to the start and end automatically.

            if (testMode) {
                // delete everything in dest
                Console.WriteLine("Deleting dest (this is for testing only)");
                DeleteEverythingInDirectory(details.DestPath);
            }

            var startDt = DateTime.Now;

            // scan for all directories and files in src
            Console.WriteLine("Scanning");
            ScanForAllDirectoriesAndFiles(details.SourcePath, ref details);

            // create all directories in desc
            CreateAllDirectories(details);

            // copy all the files in a multi-threaded fashion
            Console.WriteLine($"Starting copy-threading with {TOTAL_THREADS} threads");
            CopyAllFilesLimitedThreads(details);
            Console.WriteLine("Waiting for copy-threads to finish");

            _smartThreadPool.WaitForIdle();
            Console.WriteLine("_smartThreadPool idle achieved");

            details.TotalBytes = details.TotalBytesWrittenAllThreads();

            var endDt = DateTime.Now;
            var duration = endDt - startDt;
            Console.WriteLine($"Operation Stats... Duration: {duration}");
            Console.WriteLine($"Files: {details.Files.Count:N0}; Directories: {details.Directories.Count:N0}");
            Console.WriteLine($"Total Bytes: {details.TotalBytes:N0}; {SizeSuffix(details.TotalBytes)}");

            // 20-24 seconds, 90-98 MB/s

            var totalSeconds = (long)duration.TotalSeconds;
            var bytesPerSecond = details.TotalBytes / Math.Max(totalSeconds, 1);
            Console.WriteLine($"{bytesPerSecond:N0} bytes/sec aka {SizeSuffix(bytesPerSecond)}/sec");

            Console.WriteLine($"DONE with copy: {details.SourcePath} => {details.DestPath}");
        }

        /// <summary>
        /// This splits all the files into TOTAL_THREADS groups. It does not take into account file sizes, just file counts.
        /// It is faster to spin up TOTAL_THREADS threads and then let them go, rather than overhead of starting and ending
        /// threads over and over again. Spin up and down the threads just once.
        /// In my first iteration, I did one thread per file and determined it spent ~35% of run-time in thread overhead.
        /// </summary>
        /// <param name="details"></param>
        private static void CopyAllFilesLimitedThreads(CopyDetails details) {
            Console.WriteLine($"CopyAllFiles: {details.Files.Count:N0}");
            var filesPerThread = (details.Files.Count / TOTAL_THREADS) + 1;

            for (var i = 0; i < TOTAL_THREADS; ++i) {
                var p = new CopyDetailsThreadParam();
                p.StartIndex = i * filesPerThread;
                p.EndIndex = ((i + 1) * filesPerThread) - 1;
                p.CopyDetails = details;
                p.ThreadNumber = i;
                _smartThreadPool.QueueWorkItem(TurboCopyGT.Program.CopyFileBatch, p, WorkItemPriority.BelowNormal);
            }
        }

        private static void CopyFileBatch(CopyDetailsThreadParam param) {
            int startIndex = param.StartIndex;
            int endIndex = param.EndIndex;
            var details = param.CopyDetails;
            foreach (var sourceFilePath in details.Files.Skip(startIndex).Take(endIndex - startIndex + 1)) {
                var destFilePath = sourceFilePath.Replace(details.SourcePath, details.DestPath);
                if (sourceFilePath == destFilePath) // should never happen!?!
                    continue;

                var fi = new FileInfo(sourceFilePath);
                details.BytesWrittenPerThread[param.ThreadNumber] += fi.Length;
                //details.TotalBytes += fi.Length;
                fi.CopyTo(destFilePath);
            }
        }


        private static void CreateAllDirectories(CopyDetails details) {
            Console.WriteLine($"CreateAllDirectories: {details.Directories.Count:N0}");
            foreach (var d in details.Directories) {
                var newPath = d.Replace(details.SourcePath, details.DestPath);
                if (newPath == d)
                    continue;
                if (!Directory.Exists(newPath))
                    Directory.CreateDirectory(newPath);
            }
        }

        private static void DeleteEverythingInDirectory(string path) {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                dir.Delete(true);
            }
        }

        public static void ScanForAllDirectoriesAndFiles(string path, ref CopyDetails details) {
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
                if (DoAnyPatternsMatch(details.ExcludedFilePatterns, file.FullName))
                    continue;
                details.Files.Add(file.FullName);
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                if (DoAnyPatternsMatch(details.ExcludedDirectoryPatterns, dir.FullName))
                    continue;
                details.Directories.Add(dir.FullName);
                ScanForAllDirectoriesAndFiles(dir.FullName, ref details);
            }
        }

        private static bool DoAnyPatternsMatch(List<string> patterns, string strToCheck) {
            if (patterns == null || patterns.Count == 0)
                return false;
            return patterns.Any(exp => FileSystemName.MatchesSimpleExpression(exp, strToCheck));
        }

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

        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        static string SizeSuffix(Int64 value, int decimalPlaces = 1) {
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000) {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }


        public class CopyDetails
        {
            public CopyDetails() {
                Directories = new List<string>();
                Files = new List<string>();
                TotalBytes = 0;
                BytesWrittenPerThread = new long[TOTAL_THREADS];
                
                ExcludedDirectoryPatterns = new List<string>();
                ExcludedFilePatterns = new List<string>();
            }

            public List<string> Directories { get; set; }
            public List<string> Files { get; set; }

            public string SourcePath { get; set; }
            public string DestPath { get; set; }

            public long TotalBytes { get; set; }

            public long[] BytesWrittenPerThread { get; private set; }

            public List<string> ExcludedDirectoryPatterns { get; set; }
            public List<string> ExcludedFilePatterns { get; set; }

            public long TotalBytesWrittenAllThreads() {
                return BytesWrittenPerThread.Sum();
            }
        }

        public class CopyDetailsThreadParam
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public CopyDetails CopyDetails { get; set; }
            public int ThreadNumber { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amib.Threading;

namespace TurboCopyGT
{
    internal class CopyTasks
    {
        private static SmartThreadPool _smartThreadPool;

        private static readonly int TOTAL_THREADS = (int)Math.Round(Environment.ProcessorCount * 1.0);

        public static void CopyAction(string sourcePath, string destinationPath) {
            Console.WriteLine($"Starting {(RuntimeSettings.UseShuffle ? "shuffle " : "")}copy of {sourcePath} => {destinationPath}");
            if (!Directory.Exists(sourcePath)) {
                Console.WriteLine($"Source directory {sourcePath} does not exist. Exiting with no work performed.");
                return;
            }

            _smartThreadPool = new SmartThreadPool();
            _smartThreadPool.MaxThreads = TOTAL_THREADS;

            var details = new CopyDetails();
            details.SourcePath = sourcePath;
            details.DestPath = destinationPath;

            // figure out to read arguments in a new way so that we can do things like -xf and -xd for excluded files & directories [list]
            // when reading the X-files and dirs lists, if each individual entry does not contain a * or ?, then add * to the start and end automatically.
            
            var startDt = DateTime.Now;

            // scan for all directories and files in src
            Console.WriteLine("Scanning");
            ScanTasks.ScanForAllDirectoriesAndFiles(details.SourcePath, details);
            if (RuntimeSettings.UseShuffle)
                ScanTasks.ShuffleFilesAndDirectories(details);

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
            Console.WriteLine($"Total Bytes: {details.TotalBytes:N0}; {StringTasks.SizeSuffix(details.TotalBytes)}");
            

            var totalSeconds = (long)duration.TotalSeconds;
            var bytesPerSecond = details.TotalBytes / Math.Max(totalSeconds, 1);
            Console.WriteLine($"{bytesPerSecond:N0} bytes/sec aka {StringTasks.SizeSuffix(bytesPerSecond)}/sec");

            Console.WriteLine($"DONE with copy: {details.SourcePath} => {details.DestPath}");
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
                _smartThreadPool.QueueWorkItem(CopyTasks.CopyFileBatch, p, WorkItemPriority.BelowNormal);
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


        public class CopyDetails : IBaseActionDetails
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

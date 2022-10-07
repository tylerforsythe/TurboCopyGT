using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amib.Threading;

namespace TurboCopyGT
{
    internal class DeleteTasks
    {
        private static SmartThreadPool _smartThreadPool;

        private static readonly int TOTAL_THREADS = (int)Math.Round(Environment.ProcessorCount * 1.0);


        public static void DeleteAction(string path) {
            Console.WriteLine($"Starting {(RuntimeSettings.UseShuffle ? "shuffle " : "")}delete of path {path}");
            if (!Directory.Exists(path)) {
                Console.WriteLine($"Directory {path} does not exist. Exiting with no work performed.");
                return;
            }

            _smartThreadPool = new SmartThreadPool();
            _smartThreadPool.MaxThreads = TOTAL_THREADS;
            
            var startDt = DateTime.Now;
            
            var details = new DeleteDetails();
            details.SourcePath = path;

            // scan for all directories and files in src
            Console.WriteLine("Scanning");
            ScanTasks.ScanForAllDirectoriesAndFiles(details.SourcePath, details);
            if (RuntimeSettings.UseShuffle)
                ScanTasks.ShuffleFilesAndDirectories(details);

            Console.WriteLine($"Starting delete-threading with {TOTAL_THREADS} threads");
            DeleteAllFilesLimitedThreads(details);
            Console.WriteLine("Waiting for delete-threads to finish");

            _smartThreadPool.WaitForIdle();
            Console.WriteLine("_smartThreadPool idle achieved; now deleting all directories");

            // this deletes every directory!
            DeleteEverythingInDirectory(path);
            // now delete the one parent
            Directory.Delete(path);

            details.TotalBytes = details.TotalBytesWrittenAllThreads();

            var endDt = DateTime.Now;
            var duration = endDt - startDt;
            Console.WriteLine($"Operation Stats... Duration: {duration}");
            Console.WriteLine($"Files: {details.Files.Count:N0}; Directories: {details.Directories.Count:N0}");
            Console.WriteLine($"Total Bytes: {details.TotalBytes:N0}; {StringTasks.SizeSuffix(details.TotalBytes)}");
            

            var totalSeconds = (long)duration.TotalSeconds;
            var bytesPerSecond = details.TotalBytes / Math.Max(totalSeconds, 1);
            Console.WriteLine($"{bytesPerSecond:N0} bytes/sec aka {StringTasks.SizeSuffix(bytesPerSecond)}/sec");

            Console.WriteLine($"DONE with Delete: {details.SourcePath}");
        }

        private static void DeleteAllFilesLimitedThreads(DeleteDetails details) {
            Console.WriteLine($"DeleteAllFiles: {details.Files.Count:N0}");
            var filesPerThread = (details.Files.Count / TOTAL_THREADS) + 1;

            for (var i = 0; i < TOTAL_THREADS; ++i) {
                var p = new DeleteDetailsThreadParam();
                p.StartIndex = i * filesPerThread;
                p.EndIndex = ((i + 1) * filesPerThread) - 1;
                p.DeleteDetails = details;
                p.ThreadNumber = i;
                _smartThreadPool.QueueWorkItem(DeleteFileBatch, p, WorkItemPriority.BelowNormal);
            }
        }

        private static void DeleteFileBatch(DeleteDetailsThreadParam param) {
            int startIndex = param.StartIndex;
            int endIndex = param.EndIndex;
            var details = param.DeleteDetails;
            foreach (var sourceFilePath in details.Files.Skip(startIndex).Take(endIndex - startIndex + 1)) {
                var fi = new FileInfo(sourceFilePath);
                details.BytesWrittenPerThread[param.ThreadNumber] += fi.Length;
                fi.Delete();
            }
        }

        public static void DeleteEverythingInDirectory(string path) {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                dir.Delete(true);
            }
        }


        public class DeleteDetails : IBaseActionDetails
        {
            public DeleteDetails() {
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

            public long TotalBytes { get; set; }

            public long[] BytesWrittenPerThread { get; private set; }

            public List<string> ExcludedDirectoryPatterns { get; set; }
            public List<string> ExcludedFilePatterns { get; set; }

            public long TotalBytesWrittenAllThreads() {
                return BytesWrittenPerThread.Sum();
            }
        }

        public class DeleteDetailsThreadParam
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public DeleteDetails DeleteDetails { get; set; }
            public int ThreadNumber { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Amib.Threading;

namespace TurboCopyGT
{
    class Program
    {
        private static SmartThreadPool _smartThreadPool;

        static void Main(string[] args) {

            Console.WriteLine("Hello World!");
            _smartThreadPool = new SmartThreadPool();
            _smartThreadPool.MaxThreads = 28;

            var details = new CopyDetails();
            details.SourcePath = @"C:\temp\mblf-node-modules-20210625";
            details.DestPath = @"H:\temp\mbl_build\copy-test-destination";

            // delete everything in dest
            Console.WriteLine("Deleting dest (this is for testing only)");
            DeleteEverythingInDirectory(details.DestPath);

            var startDt = DateTime.Now;

            // scan for all directories and files in src
            Console.WriteLine("Scanning");
            ScanForAllDirectoriesAndFiles(details.SourcePath, ref details);

            // create all directories in desc
            CreateAllDirectories(details);

            // copy all the files in a multi-threaded fashion
            Console.WriteLine("Start copy-threading...");
            CopyAllFiles(details);
            Console.WriteLine("Waiting for copy-threads to finish");

            _smartThreadPool.WaitForIdle();
            Console.WriteLine("_smartThreadPool idle achieved");

            var endDt = DateTime.Now;
            var duration = endDt - startDt;
            Console.WriteLine($"Operation Stats:");
            Console.WriteLine($"Files: {details.Files.Count:N0}");
            Console.WriteLine($"Directories: {details.Directories.Count:N0}");
            Console.WriteLine($"Total Bytes: {details.TotalBytes:N0}");
            Console.WriteLine($"Bytes Formatted: {SizeSuffix(details.TotalBytes)}");
            Console.WriteLine($"Duration: {duration}");

            // 20-24 seconds, 90-98 MB/s

            var totalSeconds = (long)duration.TotalSeconds;
            var bytesPerSecond = details.TotalBytes / totalSeconds;
            Console.WriteLine($"{bytesPerSecond:N0} bytes/sec aka {SizeSuffix(bytesPerSecond)}/sec");

            Console.WriteLine($"DONE");
        }

        private static void CopyAllFiles(CopyDetails details) {
            Console.WriteLine($"CopyAllFiles: {details.Files.Count}");

            foreach (var sourceFilePath in details.Files) {
                var destFilePath = sourceFilePath.Replace(details.SourcePath, details.DestPath);
                if (sourceFilePath == destFilePath) // should never happen!?!
                    continue;

                var fi = new FileInfo(sourceFilePath);
                details.TotalBytes += fi.Length;
                //fi.CopyTo(destFilePath);
                _smartThreadPool.QueueWorkItem(System.IO.File.Copy, sourceFilePath, destFilePath);
            }
        }

        private static void CreateAllDirectories(CopyDetails details) {
            Console.WriteLine($"CreateAllDirectories: {details.Directories.Count}");
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
            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles()) {
                details.Files.Add(file.FullName);
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                details.Directories.Add(dir.FullName);
                ScanForAllDirectoriesAndFiles(dir.FullName, ref details);
            }
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
            }

            public List<string> Directories { get; set; }
            public List<string> Files { get; set; }

            public string SourcePath { get; set; }
            public string DestPath { get; set; }

            public long TotalBytes { get; set; }
        }
    }
}

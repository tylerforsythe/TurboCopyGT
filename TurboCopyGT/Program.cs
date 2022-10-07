using System;

namespace TurboCopyGT
{
    class Program
    {

        public enum AppActionMode
        {
            Copy,
            Delete,
            TestCopy
        }

        /// <param name="mode">copy or delete</param>
        /// <param name="sourcePath">For copy, the source path.</param>
        /// <param name="destinationPath">For copy, the destination of the copy. Ignored for delete.</param>
        /// <param name="deletePath">For delete, the path to delete. Ignored for copy.</param>
        /// <param name="useShuffle">Whether to shuffle the list before proceeding with the delete or copy. This turned out to be slower for most (all?) use-cases, so default is off.</param>
        static void Main(AppActionMode mode = AppActionMode.Copy, string sourcePath = "", string destinationPath = "", string deletePath = "", bool useShuffle = false) {
            RuntimeSettings.UseShuffle = useShuffle;
            switch (mode) {
                case AppActionMode.Copy: {
                    CopyTasks.CopyAction(sourcePath, destinationPath);
                    break;
                }
                case AppActionMode.Delete: {
                    DeleteTasks.DeleteAction(deletePath);
                    break;
                }
                case AppActionMode.TestCopy: {
                    Console.WriteLine($"TEST MODE: hard-coded source and dest path with shuffle {RuntimeSettings.UseShuffle}");
                    sourcePath = "";
                    destinationPath = "";
                    
                    Console.WriteLine($"TEST MODE: source {sourcePath}");
                    Console.WriteLine($"TEST MODE: destination {destinationPath}");
                    
                    // delete everything in dest
                    Console.WriteLine("Deleting dest (this is for testing only)");
                    DeleteTasks.DeleteAction(destinationPath);
                    //DeleteTasks.DeleteEverythingInDirectory(destinationPath);
                    
                    Console.WriteLine("TEST MODE: deleted destination, now performing copy test");
                    CopyTasks.CopyAction(sourcePath, destinationPath);

                    break;
                }
            }
        }
    }
}

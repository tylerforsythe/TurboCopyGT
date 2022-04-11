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
        static void Main(AppActionMode mode = AppActionMode.Copy, string sourcePath = "", string destinationPath = "", string deletePath = "") {
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
                    Console.WriteLine("TEST MODE: hard-coded source and dest path");
                    sourcePath = @"C:\temp\mblf-node-modules-20210625";
                    destinationPath = @"D:\temp\mbl_build\copy-test-destination";
                    
                    // delete everything in dest
                    Console.WriteLine("Deleting dest (this is for testing only)");
                    DeleteTasks.DeleteEverythingInDirectory(destinationPath);
                    
                    Console.WriteLine("TEST MODE: deleted destination, now performing copy test");
                    CopyTasks.CopyAction(sourcePath, destinationPath);

                    break;
                }
            }
        }
    }
}

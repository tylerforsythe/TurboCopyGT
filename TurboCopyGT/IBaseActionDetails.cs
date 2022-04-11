using System.Collections.Generic;

namespace TurboCopyGT
{
    internal interface IBaseActionDetails
    {
        public List<string> Directories { get; }
        public List<string> Files { get; }

        public List<string> ExcludedDirectoryPatterns { get; }
        public List<string> ExcludedFilePatterns { get; }
    }
}

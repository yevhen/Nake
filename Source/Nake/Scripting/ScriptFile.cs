using System.IO;

namespace Nake.Scripting
{
    class ScriptFile
    {
        public readonly string Name;
        public readonly string FullPath;
        public readonly string DirectoryPath;
        public readonly string Content;

        public ScriptFile(FileInfo file, string content)
        {
            Name = file.Name;
            FullPath = file.FullName;
            Content = content;
            DirectoryPath = file.DirectoryName;
        }
    }
}
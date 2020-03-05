using System.IO;

namespace Nake.Scripting
{
    class ScriptSource
    {
        public readonly string Content;
        public readonly FileInfo File;

        public ScriptSource(string content, FileInfo file = null)
        {
            Content = content;
            File = file;
        }

        public bool IsFile => File != null;
    }
}
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Nake.Scripting
{
    public static class ScriptAssembly
    {
        public static MetadataReference BuildReference()
        {
            return new MetadataImageReference(GetRawAssembly(), display: "Roslyn.Scripting");
        }

        public static Assembly Load()
        {
            return Assembly.Load(GetRawAssembly());
        }

        static byte[] GetRawAssembly()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Script), "Roslyn.Scripting.image"))
            {
                Debug.Assert(stream != null);

                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int) stream.Length);

                return buffer;
            }
        }
    }
}

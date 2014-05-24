using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Nake.Scripting
{
    public static class RoslynScriptingAssembly
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
            using (var stream = GetEmbeddedImage())
            {
                Debug.Assert(stream != null);

                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int) stream.Length);

                return buffer;
            }
        }

        static Stream GetEmbeddedImage()
        {
            return Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream(
                            typeof(RoslynScriptingAssembly), 
                            "Roslyn.Scripting.image");
        }
    }
}

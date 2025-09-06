using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Nake.Scripting;

public class AssemblyReference
{
    public readonly string FullPath;
    public readonly string Name;

    public AssemblyReference(MetadataReference reference)
        : this(reference.Display ?? throw new ArgumentException("MetadataReference.Display cannot be null", nameof(reference)))
    {}

    public AssemblyReference(string fullPath)
    {
        Debug.Assert(!string.IsNullOrEmpty(fullPath));

        FullPath = fullPath;
        Name = Path.GetFileNameWithoutExtension(fullPath);
    }
}
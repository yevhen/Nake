using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Nake.Scripting
{
	public class AssemblyReference
	{
		public readonly string FullPath;
		public readonly string Name;

		public AssemblyReference(MetadataReference reference)
			: this(reference.Display)
		{ }

		public AssemblyReference(string fullPath)
		{
			Debug.Assert(!string.IsNullOrEmpty(fullPath));

			FullPath = fullPath;
			Name = Path.GetFileNameWithoutExtension(fullPath);
		}
	}
}
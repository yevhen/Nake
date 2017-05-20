using System.Collections.Generic;
using System.Runtime.Loader;

namespace Nake.Scripting
{
	public static class AssemblyResolver
	{
		static readonly Dictionary<string, AssemblyReference> references =
		   new Dictionary<string, AssemblyReference>();

		public static void Add(AssemblyReference reference)
		{
			references[reference.Name] = reference;
		}

		public static void Register()
		{
			AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
			{
				var reference = references.Find(new AssemblyReference(assemblyName.Name).Name);

				return  reference != null
					? AssemblyLoadContext.Default.LoadFromAssemblyPath(reference.FullPath)
					: null;
			};
		}
	}
}
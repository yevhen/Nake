using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nake.Scripting
{
	static class GAC
	{
		static readonly IAssemblyCache gac;

		static GAC()
		{
			var result = GacApi.CreateAssemblyCache(out gac, 0);
			if (result != IntPtr.Zero)
				Marshal.ThrowExceptionForHR(result.ToInt32());
		}

		public static bool AssemblyExist(string assemblyname, out string path)
		{
			try
			{
				path = QueryAssemblyInfo(assemblyname);
				return true;
			}
			catch (System.IO.FileNotFoundException e)
			{
				path = e.Message;
				return false;
			}
		}

		//
		// If assemblyName is not fully qualified, a random matching (in reality its a highest version)  may be 
		//

		static string QueryAssemblyInfo(string assemblyName)
		{
			var assembyInfo = new AssemblyInfo { cchBuf = 512 };
			assembyInfo.currentAssemblyPath = new String(' ', assembyInfo.cchBuf);

			var result = gac.QueryAssemblyInfo(1, assemblyName, ref assembyInfo);
			if (result != IntPtr.Zero)
				Marshal.ThrowExceptionForHR(result.ToInt32());

			return assembyInfo.currentAssemblyPath;
		}
	}

	internal class GacApi
	{
		[DllImport("fusion.dll")]
		internal static extern IntPtr CreateAssemblyCache(
			out IAssemblyCache ppAsmCache, int reserved);
	}

	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
	Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
	internal interface IAssemblyCache
	{
		int Dummy1();
		[PreserveSig()]
		IntPtr QueryAssemblyInfo(
			int flags,
			[MarshalAs(UnmanagedType.LPWStr)]
			String assemblyName,
			ref AssemblyInfo assemblyInfo);

		int Dummy2();
		int Dummy3();
		int Dummy4();
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct AssemblyInfo
	{
		public int cbAssemblyInfo;
		public int assemblyFlags;
		public long assemblySizeInKB;

		[MarshalAs(UnmanagedType.LPWStr)]
		public String currentAssemblyPath;

		public int cchBuf;
	}
}
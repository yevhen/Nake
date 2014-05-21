#r "Tools\Nake\Meta.dll"
#r "Tools\Nake\Utility.dll"

#r "System.Xml"
#r "System.Xml.Linq"
#r "System.IO.Compression"
#r "System.IO.Compression.FileSystem"

#r "Packages\EasyHttp.1.6.58.0\lib\net40\EasyHttp.dll"
#r "Packages\JsonFx.2.0.1209.2802\lib\net40\JsonFx.dll"

using Nake;

using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Dynamic;
using System.Collections.Generic;

using EasyHttp.Http;
using EasyHttp.Infrastructure;

static string OutputPath = @"$NakeScriptDirectory$\Output";
static string PackagePath = @"{OutputPath}\Package";

static string DebugOutputPath = @"{PackagePath}\Debug";
static string ReleaseOutputPath = @"{PackagePath}\Release";

static Func<string> PackageFile = () => PackagePath + @"\Nake.{Version()}.nupkg";
static Func<string> ArchiveFile = () => OutputPath + @"\{Version()}.zip";

/// <summary> 
/// Zips all binaries for standalone installation
/// </summary>
[Task] public static void Zip()
{
	var files = new FileSet
	{
		@"{ReleaseOutputPath}\Nake.*",
		@"{ReleaseOutputPath}\Meta.*",
		@"{ReleaseOutputPath}\Utility.*",
		@"{ReleaseOutputPath}\GlobDir.dll",
		@"{ReleaseOutputPath}\Microsoft.CodeAnalysis.dll",
		@"{ReleaseOutputPath}\Microsoft.CodeAnalysis.CSharp.dll",
        @"{ReleaseOutputPath}\System.Collections.Immutable.dll",
        @"{ReleaseOutputPath}\System.Reflection.Metadata.dll",
		"-:*.Tests.*"
	};

	FS.Delete(ArchiveFile());

	using (ZipArchive archive = ZipFile.Open(ArchiveFile(), ZipArchiveMode.Create))
	{
		foreach (var file in files)
			archive.CreateEntryFromFile(file, Path.GetFileName(file));
	}
}

/// <summary>
/// Publishes package to NuGet gallery
/// </summary>
[Task] public static void NuGet()
{
	Cmd.Exec(@"Tools\Nuget.exe push {PackageFile()} $NuGetApiKey$");
}


/// <summary> 
/// Publishes standalone version to GitHub releases
/// </summary>
[Task] public static void Standalone(bool beta, string branch, string description = null)
{
	string release = CreateRelease(beta, branch, description);

	Upload(release, ArchiveFile(), "application/zip");
}

static string CreateRelease(bool beta, string branch, string description)
{
	dynamic data = new ExpandoObject();

	data.tag_name = data.name = Version();
	data.target_commitish = branch;
	data.prerelease = beta;
    data.body = !string.IsNullOrEmpty(description) 
                ? description 
                : "Standalone release {Version()}";

	return GitHub().Post("https://api.github.com/repos/yevhen/nake/releases",
						  data, HttpContentTypes.ApplicationJson).Location;
}

static void Upload(string release, string filePath, string contentType)
{
	GitHub().Post(GetUploadUri(release) + "?name=" + Path.GetFileName(filePath), null, new List<FileData>
	{
		new FileData()
		{
			ContentType = contentType,
			Filename = filePath
		}
	});
}

static string GetUploadUri(string release)
{
	var body = GitHub().Get(release).DynamicBody;
	return ((string)body.upload_url).Replace("{{?name}}", "");
}

static HttpClient GitHub()
{
	var client = new HttpClient();

	client.Request.Accept = "application/vnd.github.manifold-preview";
	client.Request.ContentType = "application/json";
	client.Request.AddExtraHeader("Authorization", "token $GitHubToken$");

	return client;
}

static string Version()
{
	return FileVersionInfo
			.GetVersionInfo(@"{ReleaseOutputPath}\Nake.exe")
			.FileVersion;
}
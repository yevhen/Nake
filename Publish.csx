#r "Packages\EasyHttp.1.6.58.0\lib\net40\EasyHttp.dll"
#r "Packages\JsonFx.2.0.1209.2802\lib\net40\JsonFx.dll"
#r "Packages\DotNetZip.1.9.2\lib\net20\Ionic.Zip.dll"

using Nake;
using Nake.FS;
using Nake.Cmd;

using System.Diagnostics;
using System.Dynamic;

using EasyHttp.Http;
using EasyHttp.Infrastructure;

using Ionic.Zip;

static string OutputPath = @"$NakeScriptDirectory$\Output";
static string PackagePath = @"{OutputPath}\Package";

static string DebugOutputPath = @"{PackagePath}\Debug";
static string ReleaseOutputPath = @"{PackagePath}\Release";

Func<string> PackageFile = () => PackagePath + @"\Nake.{Version()}.nupkg";
Func<string> ArchiveFile = () => OutputPath + @"\{Version()}.zip";

/// Zips all binaries for standalone installation
[Task] void Zip()
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

    Delete(ArchiveFile());

    using (ZipFile archive = new ZipFile())
    {
        foreach (var file in files)
            archive.AddFile(file, "");

        archive.Save(ArchiveFile());
    }
}

/// Publishes package to NuGet gallery
[Task] void NuGet()
{
    Exec(@"Tools\Nuget.exe push {PackageFile()} $NuGetApiKey$");
}

/// Publishes standalone version to GitHub releases
[Task] void Standalone(bool beta, string branch, string description = null)
{
    Zip();

    string release = CreateRelease(beta, branch, description);
    Upload(release, ArchiveFile(), "application/zip");
}

string CreateRelease(bool beta, string branch, string description)
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

void Upload(string release, string filePath, string contentType)
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

string GetUploadUri(string release)
{
    var body = GitHub().Get(release).DynamicBody;
    return ((string)body.upload_url).Replace("{{?name}}", "");
}

HttpClient GitHub()
{
    var client = new HttpClient();

    client.Request.Accept = "application/vnd.github.manifold-preview";
    client.Request.ContentType = "application/json";
    client.Request.AddExtraHeader("Authorization", "token $GitHubToken$");

    return client;
}

string Version()
{
    return FileVersionInfo
            .GetVersionInfo(@"{ReleaseOutputPath}\Nake.exe")
            .ProductVersion;
}
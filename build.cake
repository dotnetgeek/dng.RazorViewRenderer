#addin nuget:?package=Cake.MinVer&version=2.0.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var publishDir = Directory (Argument("publishDir", EnvironmentVariable("BUILD_PUBLISH") ?? "./publish"));; 
var publishNuPkgDir = Directory(publishDir) + Directory("nupkg");

DotNetBuildSettings DotNetBuildSettings; 
DotNetMSBuildSettings msBuildSettings;

Setup(context =>
{
    var version = MinVer(settings => settings.WithMinimumMajorMinor("1.0").WithTagPrefix("v"));

    Information("dng.RazorViewRenderer");
    Information($"Configuration: {configuration}");
    Information($"Version: {version.Version}");
    Information($"FileVersion: {version.FileVersion}");
    Information($"AssemblyVersion: {version.AssemblyVersion}");
    Information($"PackageVersion: {version.PackageVersion}");

    msBuildSettings = new DotNetMSBuildSettings()
        .SetFileVersion(version.FileVersion)
        .SetInformationalVersion(version.AssemblyVersion.ToString())
        .SetVersion(version.Version.ToString())
        .WithProperty("PackageVersion", version.PackageVersion.ToString());

    DotNetBuildSettings = new DotNetBuildSettings
    {
        Configuration = configuration,
        MSBuildSettings = msBuildSettings
    };
});

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./src/**/obj");
	CleanDirectories("./src/**/bin");
	CleanDirectory(publishDir);
});

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    DotNetRestore("./",new DotNetRestoreSettings
    {
        Sources = new [] {
            "https://api.nuget.org/v3/index.json"
        }
    });
});

Task("Build")
    .Does(() =>
{
    DotNetBuild("./dng.RazorViewRenderer.sln", DotNetBuildSettings);
});

Task("Create-NuGet-Package")
    .Does(() => 
{
    Information("Publish Directory: {0}", MakeAbsolute(publishDir));
  //  var publishDirBuild = Directory(publishDir) + Directory("build");
    DotNetPack("./src/dng.RazorViewRenderer/dng.RazorViewRenderer.csproj", new DotNetPackSettings
    {
        Configuration = configuration,
        OutputDirectory = publishNuPkgDir,
        MSBuildSettings = msBuildSettings
    });

    Information("NuGet Package created.");
});

Task("Push-To-NuGet")
	.Does(()=> {

    var nugetServer = EnvironmentVariable("nuget_server") ?? "";
    var nugetApiKey = EnvironmentVariable("nuget_apikey") ?? "";

    if (string.IsNullOrEmpty(nugetServer))
    {
        Error("Nuget-Server not definied.");
        return;
    }

    var packages = GetFiles($"{publishDir}/**/*.nupkg");
    foreach(var package in packages)
    {
        Information($"NuGet Package {package} found to push");
        NuGetPush(package, new NuGetPushSettings {
            Source = nugetServer,
            ApiKey = nugetApiKey
        });
    }
});

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build");

Task("Pack")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build")
    .IsDependentOn("Create-NuGet-Package");

Task("Publish")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build")
    .IsDependentOn("Create-NuGet-Package")
    .IsDependentOn("Push-To-NuGet");

RunTarget(target);
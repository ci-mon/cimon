using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Powershell;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string AppPoolName { get; set; } = "cimon";

    public ConvertableDirectoryPath CimonProject { get; set; }

    public DirectoryPath PublishDir { get; set; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        CimonProject = context.Directory("../Cimon/Cimon.csproj");
        PublishDir = context.Argument("publish-dir", "../../output/cimon");
    }
}

[TaskName("Publish")]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) {
        context.DotNetPublish(context.CimonProject, new DotNetPublishSettings {
            Configuration = "Release",
            Runtime = "win-x64",
            SelfContained = true,
            OutputDirectory = context.PublishDir
        });
    }
}

[TaskName("Deploy")]
[IsDependentOn(typeof(PublishTask))]
public sealed class DeployTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) {
        var deployServerName = context.Argument<string>("deploy-server-name");
        var deployPath = context.Argument<string>("deploy-path");
        var ci = context.Argument("ci", true);
        var stopPoolScript =
            $$"""
                Stop-WebAppPool -Name "{{context.AppPoolName}}";
                $WorkerProcesses = & "$env:SystemRoot\system32\inetsrv\appcmd.exe" list wp
                $pattern = 'WP "(\d+)" \(applicationPool:{{context.AppPoolName}}\)'
                $match = [regex]::Match($WorkerProcesses, $pattern)
                if ($match.Success) {
                    $ProcessId = $match.Groups[1].Value
                    Write-Host "Killing process ID: $ProcessId"
                    # Kill the process
                    $counter = 15;
                    Write-Host "Waiting for process to stop"
                    $processStopped = $false
                    while ($counter -gt 0) {
                        $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
                        if ($process -eq $null) {
                            $processStopped = $true
                            break
                        }
                        Start-Sleep 1
                        $counter--;
                    }
                    if (-not $processStopped) {
                        Write-Host "Process did not stop in time, killing it"
                        Stop-Process -Id $ProcessId -Force
                    }
                }
            """;
        context.StartPowershellScript(stopPoolScript, new PowershellSettings {
            ComputerName = deployServerName,
            Modules = new[]{"webadministration"},
            ExceptionOnScriptError = false,
            OutputToAppConsole = !ci,
            LogOutput = ci
        });
        context.Information("Synchronizing files");
        var webConfig = "web.config";
        context.StartProcess("robocopy", new ProcessSettings {
            Arguments = new ProcessArgumentBuilder()
                .AppendQuoted(context.PublishDir.FullPath)
                .AppendQuoted(deployPath)
                .Append("/MIR")
                .Append("/R:15")
                .Append("/W:5")
                .Append("/XD").AppendQuoted("nativeApps").AppendQuoted("db").AppendQuoted("logs")
                .Append("/XF").AppendQuoted(webConfig)
                .Append("/XF").AppendQuoted("appsettings.Production.json")
        });
        var webConfigPath = context.File(webConfig);
        var webConfigDestination = context.Directory(deployPath) + webConfigPath;
        if (!context.FileExists(webConfigDestination)) {
             context.CopyFile(context.PublishDir.GetFilePath(webConfig), webConfigDestination);
        }
        context.StartPowershellScript($"Start-WebAppPool -Name \"{context.AppPoolName}\"", new PowershellSettings {
            ComputerName = deployServerName,
            Modules = new[]{"webadministration"},
            OutputToAppConsole = !ci,
            LogOutput = ci
        });
        context.Log.Information("Cimon app deployed");
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(DeployTask))]
public class DefaultTask : FrostingTask
{
}

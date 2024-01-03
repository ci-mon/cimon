using System.Threading.Tasks;
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
        var stopPoolScript = 
            $$"""
              $WorkerProcesses = & "$env:SystemRoot\system32\inetsrv\appcmd.exe" list wp
              Stop-WebAppPool -Name "{{context.AppPoolName}}";
              sleep 5;
              if ($wp -match "WP ""(\d+)"" \(applicationPool:{{context.AppPoolName}}\)") {
                  $ProcessId = $matches[1]
                  Write-Host "Killing process ID: $ProcessId"
                  
                  # Kill the process
                  Stop-Process -Id $ProcessId -Force
              }
          """;
        context.StartPowershellScript(stopPoolScript, new PowershellSettings {
            ComputerName = deployServerName,
            Modules = new[]{"webadministration"},
            ExceptionOnScriptError = false
        });
        context.Information("Synchronizing files");
        context.StartProcess("robocopy", new ProcessSettings {
            Arguments = new ProcessArgumentBuilder()
                .AppendQuoted(context.PublishDir.FullPath)
                .AppendQuoted(deployPath)
                .Append("/MIR")
                .Append("/R:5")
                .Append("/W:5")
                .Append("/XD").AppendQuoted("nativeApps").AppendQuoted("db")
        });
        context.StartPowershellScript($"Start-WebAppPool -Name \"{context.AppPoolName}\"", new PowershellSettings {
            ComputerName = deployServerName,
            Modules = new[]{"webadministration"}
        });
        context.Log.Information("Cimon app deployed");
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(DeployTask))]
public class DefaultTask : FrostingTask
{
}
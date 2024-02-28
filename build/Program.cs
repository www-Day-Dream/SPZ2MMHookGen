using System;
using System.Linq;
using System.Threading.Tasks;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Solution.Project;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

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
    public string TargetProject { get; }
    public string TargetConfiguration { get; }
    public string InstallDir { get; }
    
    public BuildContext(ICakeContext context)
        : base(context)
    {
        context.Environment.WorkingDirectory = context.Environment.WorkingDirectory.GetParent();

        TargetProject = context.Argument("Project", "Main");
        TargetConfiguration = context.Argument("Configuration", "Release");
        
        if (context.Argument("Clean", false))
            context.CleanDirectory(context.Environment.WorkingDirectory.Combine(TargetProject + "/bin/Release"));

        var installLoc = context.Argument("Install", string.Empty);
        if (installLoc != string.Empty)
            InstallDir = context.ExpandEnvironmentVariables(new DirectoryPath(installLoc)).Collapse().ToString();
        
        context.Log.Information(InstallDir != null && context.DirectoryExists(InstallDir));
    }
}

[TaskName("Build")]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var slnFilePath = context.GetFiles("*.sln").FirstOrDefault();
        
        var mainProject = context.ParseSolution(slnFilePath).Projects
            .FirstOrDefault(proj => proj.Name == context.TargetProject);
        if (mainProject == null)
        {
            context.Log.Error("No main project named " + context.TargetProject + " exists!");
            return;
        }
        
        context.DotNetPublish(mainProject.Path.ToString(), new DotNetPublishSettings()
        {
            Configuration = context.TargetConfiguration
        });
        
        var outputPath = mainProject.Path.GetDirectory().Combine("./bin/" + context.TargetConfiguration + "/");
        var childPath = context.GetSubDirectories(outputPath).FirstOrDefault();
        if (childPath == null) return;
        
        var netVersionPath = childPath;
        var publishOutput = netVersionPath.Combine("./publish/");
        var patchersPath = netVersionPath.Combine("./patchers/");
        var depsOutput = patchersPath.Combine("./dynamic_deps/");

        context.DeleteFiles(publishOutput + "/System.*.dll");
        
        context.CopyDirectory(publishOutput, depsOutput);
        
        context.CopyFiles(netVersionPath + "/" + context.TargetProject + ".dll", patchersPath);
        context.CopyFiles(netVersionPath + "/*.txt", patchersPath);
        
        context.DeleteDirectory(publishOutput, new DeleteDirectorySettings() {Force = true, Recursive = true});
        context.DeleteFile(depsOutput.CombineWithFilePath(context.TargetProject + ".dll"));
        context.DeleteFiles(netVersionPath + "/*.*");

        var outputZip = outputPath.CombineWithFilePath("Shapez2AppDataExtract.zip");
        context.Zip(netVersionPath, outputZip);
        
        context.Log.Information("Build completed successfully!");

        if (context.InstallDir != null)
        {
            context.Log.Information("Installing to " + context.InstallDir + "/..");
            
            context.Unzip(outputZip, context.InstallDir, true);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using MonoMod.RuntimeDetour.HookGen;

namespace SPZ2MMHookGen
{
    public class Patcher : IPatcher
    {
        private int NumberOfDependencies { get; set; }
        private int NumberResolved { get; set; }
        private string DynamicDepsPath { get; set; }
        private string SPZ2Path { get; set; }

        private string[] AssembliesToGen { get; } = new string[] {
            "SPZGameAssembly.dll", "Core.dll"
        };
        
        public void Patch()
        {
            SPZ2Path = Environment.GetEnvironmentVariable("SPZ2_PERSISTENT");
            var spz2ManagedDir = Environment.GetEnvironmentVariable("SPZ2_PATH");
            
            if (SPZ2Path == null || spz2ManagedDir == null)
                throw new ArgumentNullException();

            Environment.SetEnvironmentVariable("MONOMOD_HOOKGEN_PRIVATE", "1");
            Environment.SetEnvironmentVariable("MONOMOD_DEPENDENCY_MISSING_THROW", "0");

            DynamicDepsPath = Path.Combine(Path.Combine(SPZ2Path,
                GameEnvironmentManager.PATCHERS_PATH), "dynamic_deps");
            if (!Directory.Exists(DynamicDepsPath)) Directory.CreateDirectory(DynamicDepsPath);
            
            NumberOfDependencies = Directory.GetFiles(DynamicDepsPath, "*.dll").Length;
            
            AppDomain.CurrentDomain.AssemblyResolve += ResolveDynamicDeps;

            foreach (var assemName in AssembliesToGen)
                GenerateFor(Path.Combine(spz2ManagedDir, assemName));
        }

        private void GenerateFor(string pathToDll)
        {
            var fileName = Path.GetFileName(pathToDll);
            var outputDll = Path.Combine(Directory.GetParent(pathToDll)?.ToString() ?? "", "MMHook." + Path.GetFileName(pathToDll));
            
            if (File.Exists(outputDll) && File.GetLastWriteTimeUtc(outputDll) == File.GetLastWriteTimeUtc(pathToDll))
            {
                Console.WriteLine("Skipping HookGen for '" + fileName +
                                      "' because it hasn't been updated since last time.");
                return;
            }

            if (File.Exists(outputDll))
                File.Delete(outputDll);

            using (var monoModder = new MonoMod.MonoModder())
            {
                monoModder.InputPath = pathToDll;
                monoModder.OutputPath = outputDll;
                monoModder.DependencyDirs = new List<string>()
                {
                    DynamicDepsPath, Environment.GetEnvironmentVariable("SPZ2_PATH")
                };
                monoModder.ReadingMode = ReadingMode.Deferred;
                monoModder.PublicEverything = true;
                monoModder.Read();
                monoModder.MapDependencies();
                
                monoModder.Log("[HookGen] Starting HookGenerator for " + fileName);

                var hookGenerator = new HookGenerator(monoModder, Path.GetFileName(outputDll));
                using (var outputModule = hookGenerator.OutputModule)
                {
                    hookGenerator.Generate();
                    
                    monoModder.Log("[HookGen] " + outputModule.Assembly.Name);
                    outputModule.Write(outputDll);
                    
                    monoModder.Log("[HookGen] Done processing " + fileName);
                }
            }
            
            File.SetLastWriteTimeUtc(outputDll, File.GetLastWriteTimeUtc(pathToDll));
        }

        private Assembly ResolveDynamicDeps(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            Assembly assembly;
            try
            {
                var withDll = Path.Combine(DynamicDepsPath, assemblyName.Name + ".dll");
                var withExe = Path.Combine(DynamicDepsPath, assemblyName.Name + ".exe");
                assembly = Assembly.LoadFile(File.Exists(withDll) ? withDll : withExe);
            }
            catch (Exception)
            {
                assembly = null;
            }

            if (assembly == null) return null;
            
            NumberResolved += assemblyName.Name.StartsWith("MMHook.") ? 0 : 1;
            
            return assembly;
        }
    }
}
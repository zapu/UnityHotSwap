using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace UnityHotSwap
{
    internal class UnityCompiler
    {
        public UnityCompiler() {
        }

        public static string TempPath {
            get {
                return Path.GetFullPath(Application.dataPath + "/../Temp/");
            }
        }

        public static string MainAssemblyFilename {
            get {
                return Path.GetFullPath(Application.dataPath + "/../Library/ScriptAssemblies/Assembly-CSharp.dll");
            }
        }

        public static void Trace(string str) {
            ILDynaRec.Debug.Trace(str);
        }

        private string GetMonoCompileParams(string assembly, string outFilename) {
            var dir = new DirectoryInfo(TempPath);
            var fileInfos = dir.GetFileSystemInfos("UnityTempFile-*");

            Array.Sort(fileInfos, (a, b) => b.CreationTime.CompareTo(a.CreationTime));

            var asmPath = $"-out:Temp/{assembly}";
            foreach (var fileInfo in fileInfos) {
                try {
                    var text = File.ReadAllText(fileInfo.FullName);
                    if (text.Contains(asmPath)) {
                        return text.Replace(asmPath, "-out:" + outFilename);
                    }
                }
                catch {
                }
            }

            return null;
        }

        public bool InvokeCompiler(string assemblyName, string outputName) {
            var compileParams = GetMonoCompileParams(assemblyName, $"Temp/{outputName}");
            if (compileParams == null) {
                ILDynaRec.Debug.Log($"No compile params for {assemblyName}. " +
                    "It's likely that assembly has yet to be compiled during current session");
                return false;
            }

            var file = File.CreateText(TempPath + "HotPatchTemp");
            file.Write(compileParams);
            file.Close();

            var process = new Process();

            OutputAssemblyPath = Path.GetFullPath(TempPath + "/" + outputName);

            if (File.Exists(OutputAssemblyPath)) {
                File.Delete(OutputAssemblyPath);
            }

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            //	Command line:
            // "C:\Program Files\Unity\Editor\Data\MonoBleedingEdge\bin\mono.exe"
            // "C:\Program Files\Unity\Editor\Data\MonoBleedingEdge\lib\mono\4.5\mcs.exe"
            // @Temp/UnityTempFile-2dd7cda01d4f8c6428fa4253a6886c18

            process.StartInfo.FileName = @"C:\Program Files\Unity\Editor\Data\MonoBleedingEdge\bin\mono.exe";
            process.StartInfo.Arguments = "\"" + @"C:\Program Files\Unity\Editor\Data\MonoBleedingEdge\lib\mono\4.5\mcs.exe" + "\"" + " @Temp/HotPatchTemp";

            Trace($"Running {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            process.Start();
            process.WaitForExit();

            return File.Exists(OutputAssemblyPath);
        }

        public string OutputAssemblyPath { get; private set; }
    }
}
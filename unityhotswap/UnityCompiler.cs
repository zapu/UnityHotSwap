using System;
using UnityEditor;
using UnityEngine;

using System.IO;
using System.Diagnostics;

namespace UnityHotSwap
{
    class UnityCompiler
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

        string GetMonoCompileParams(string outFilename) {
            var dir = new DirectoryInfo(TempPath);
            var fileInfos = dir.GetFileSystemInfos("UnityTempFile-*");

            Array.Sort(fileInfos, (a, b) => b.CreationTime.CompareTo(a.CreationTime));

            foreach (var fileInfo in fileInfos) {
                try {
                    var text = File.ReadAllText(fileInfo.FullName);
                    if (text.Contains("-out:Temp/Assembly-CSharp.dll")) {
                        return text.Replace("-out:Temp/Assembly-CSharp.dll", "-out:" + outFilename);
                    }
                }
                catch {

                }
            }

            return null;
        }

        public bool InvokeCompiler() {
            var compileParams = GetMonoCompileParams("Temp/Assembly-HotPatch-CSharp.dll");
            if (compileParams == null) {
                UnityEngine.Debug.Log("No compile params. Project was never compiled in this session?");
                return false;
            }

            var file = File.CreateText(TempPath + "HotPatchTemp");
            file.Write(compileParams);
            file.Close();

            var process = new Process();

            OutputAssemblyPath = Path.GetFullPath(TempPath + "/Assembly-HotPatch-CSharp.dll");

            if (File.Exists(OutputAssemblyPath)) {
                File.Delete(OutputAssemblyPath);
            }

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.FileName = @"C:\Program Files\Unity\Editor\Data\Mono\bin\mono.exe";
            process.StartInfo.Arguments = "\"" + @"C:\Program Files\Unity\Editor\Data\Mono\lib\mono\2.0\gmcs.exe" + "\"" + " @Temp/HotPatchTemp";

            process.Start();
            process.WaitForExit();

            return File.Exists(OutputAssemblyPath);
        }

        public string OutputAssemblyPath { get; private set; }
    }
}


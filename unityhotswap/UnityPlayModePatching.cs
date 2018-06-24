using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityHotSwap
{
    [InitializeOnLoad]
    public static class UnityPlayModePatching
    {
        static UnityPlayModePatching() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += DisableDefaultRecompilation;

            var prefix = "<b>[hotp]</b> ";
            ILDynaRec.Debug.LogAction = (str) => Debug.Log(prefix + str);
            ILDynaRec.Debug.WarnAction = (str) => Debug.LogWarning(prefix + str);
            ILDynaRec.Debug.ErrorAction = (str) => Debug.LogError(prefix + str);

            var logFilename = UnityCompiler.TempPath + "/hotswap.log";

            //File.Delete(logFilename);

            File.WriteAllText(logFilename, "");
            System.Diagnostics.Trace.Listeners.Clear();
            System.Diagnostics.Trace.Listeners.Add(
                new System.Diagnostics.TextWriterTraceListener(logFilename, "file log"));
            System.Diagnostics.Trace.AutoFlush = true;
        }

        static ILDynaRec.HotPatcher patcher;
        private static List<string> loadedAssemblies = new List<string>();

        static void OnPlayModeStateChanged(PlayModeStateChange stateChange) {
            if (stateChange == PlayModeStateChange.ExitingPlayMode) {
                patcher = null;
                loadedAssemblies.Clear();
            }
        }

        static void DisableDefaultRecompilation(PlayModeStateChange stateChange) {
            switch (stateChange) {
                case PlayModeStateChange.EnteredPlayMode: {
                        EditorApplication.LockReloadAssemblies();
                        ILDynaRec.Debug.Log("<i>Assembly Reload locked as entering play mode</i>");
                        break;
                    }
                case PlayModeStateChange.ExitingPlayMode: {
                        ILDynaRec.Debug.Log("<i>Assembly Reload unlocked as exiting play mode</i>");
                        EditorApplication.UnlockReloadAssemblies();
                        break;
                    }
            }
        }

        public static string ScriptAssembliesDir {
            get {
                return Path.GetFullPath(Application.dataPath + "/../Library/ScriptAssemblies/");
            }
        }

        static void LoadAllAssemblies() {
            EditorUtility.DisplayCancelableProgressBar("Hot patching", "Initial assembly load", 0.0f);

            var dir = new DirectoryInfo(ScriptAssembliesDir);
            var fileInfos = dir.GetFileSystemInfos("*.dll");

            int i = 0;
            foreach (var fileInfo in fileInfos) {
                if (EditorUtility.DisplayCancelableProgressBar("Hot patching", $"Analysing {fileInfo.Name}", (float)(i++) / fileInfos.Length)) {
                    throw new UnityException("Hotpatch cancelled");
                }
                patcher.LoadLocalAssembly(fileInfo.FullName);
                loadedAssemblies.Add(fileInfo.Name);
                UnityCompiler.Trace($"Read {fileInfo.Name} ({fileInfo.FullName})");
            }
        }

        public static void HotPatch(string assemblyName) {
            if (!EditorApplication.isPlaying) {
                return;
            }

            try {
                if (patcher == null) {
                    patcher = new ILDynaRec.HotPatcher();
                    LoadAllAssemblies();
                }

                var checkAssemblies = loadedAssemblies.ToArray();
                if(assemblyName != null) {
                    checkAssemblies = checkAssemblies.Where((name) => name == assemblyName).ToArray();
                }

                int i = 0;
                foreach (var assembly in checkAssemblies) {
                    if (EditorUtility.DisplayCancelableProgressBar("Hot patching", $"Compiling {assembly}", (float)(i++) / checkAssemblies.Length)) {
                        throw new UnityException("Hotpatch cancelled");
                    }

                    var compiler = new UnityCompiler();
                    var outputName = Path.GetFileNameWithoutExtension(assembly) + "--hotpatch.dll";
                    if (!compiler.InvokeCompiler(assembly, outputName)) {
                        UnityCompiler.Trace($"Failed to compile {assembly}.");
                        continue;
                    }

                    UnityCompiler.Trace($"Compiled assembly {assembly} as {compiler.OutputAssemblyPath}, running hot patcher.");
                    patcher.HotPatch(compiler.OutputAssemblyPath);
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Experimental/Hot-patch project &F10", isValidateFunction: true)]
        private static bool HotPatchMenuVal() {
            return EditorApplication.isPlaying;
        }

        [MenuItem("Experimental/Hot-patch project &F10")]
        private static void HotPatchMenu() {
            HotPatch(null);
        }

#if FALSE
        // TODO: Disabled. Breaks assembly reloading lock.

        [MenuItem("Assets/Hot-patch assembly", isValidateFunction: true)]
        public static bool HotPatchAssemblyVal() {
            if (!EditorApplication.isPlaying || Selection.activeObject == null) {
                return false;
            }
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return Path.GetExtension(path) == ".asmdef";
        }

        [MenuItem("Assets/Hot-patch assembly")]
        public static void HotPatchAssembly() {
            if (!EditorApplication.isPlaying || Selection.activeObject == null) {
                return;
            }
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            HotPatch(Path.ChangeExtension(Path.GetFileName(path), "dll"));
        }
#endif
    }
}

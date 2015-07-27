using UnityEngine;
using UnityEditor;

namespace UnityHotSwap
{
    public class HotpatchSettingsWindow : EditorWindow
    {
        bool AutolockRefresh = EditorPrefs.GetBool("AutolockRefresh");
        bool HotPatchInstrument = EditorPrefs.GetBool("HotPatchInstrument");

        void OnGUI() {
            bool value;

            value = EditorGUILayout.Toggle("Autolock refresh", AutolockRefresh);
            if (value != AutolockRefresh) {
                AutolockRefresh = value;
                EditorPrefs.SetBool("AutolockRefresh", value);
            }

            value = EditorGUILayout.Toggle("Hotpatching", HotPatchInstrument);
            if (value != HotPatchInstrument) {
                HotPatchInstrument = value;
                EditorPrefs.SetBool("HotPatchInstrument", value);

                if (value) {
                    AssemblyPostprocessor.Start();
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class SettingsMenus
    {
        [MenuItem("Tools/Hot-swap Settings")]
        private static void ShowSettingsMenu() {
            var window = EditorWindow.GetWindow<HotpatchSettingsWindow>();
            window.Show();
        }

        [MenuItem("Tools/Hot-swap &F10", true)]
        private static bool HotPatchMenuV() {
            return EditorPrefs.GetBool("HotPatchInstrument") && EditorApplication.isPlaying;
        }

        [MenuItem("Tools/Hot-swap &F10")]
        private static void HotPatchMenu() {
            AssemblyPostprocessor.HotPatch();
        }
    }
}
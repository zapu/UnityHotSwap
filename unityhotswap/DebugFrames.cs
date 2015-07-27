using System;
using System.Text;

namespace UnityHotSwap
{
    static class DebugFrames
    {
        static StringBuilder currentFrame;

        public static void StartFrame() {
            currentFrame = new StringBuilder();

            ILDynaRec.Debug.LogAction = (str) => currentFrame.AppendLine(str);
            ILDynaRec.Debug.WarnAction = (str) => currentFrame.AppendLine(str);
            ILDynaRec.Debug.ErrorAction = (str) => currentFrame.AppendLine(str);
        }

        public static void EndFrameLog() {
            UnityEngine.Debug.Log(currentFrame.ToString());
        }

        public static void EndFrameWarning() {
            UnityEngine.Debug.LogWarning(currentFrame.ToString());
        }

        public static void EndFrameError() {
            UnityEngine.Debug.LogError(currentFrame.ToString());
        }
    }
}

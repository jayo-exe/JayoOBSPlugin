using UnityEngine;

namespace JayoOBSPlugin
{
    static class Logger
    {
        private static string identifier = "OBS Plugin";
        public static void LogInfo(object message) => Debug.Log($"[{identifier}] {message}");
        public static void LogWarning(object message) => Debug.LogWarning($"[{identifier}] {message}");
        public static void LogError(object message) => Debug.LogError($"[{identifier}] {message}");
    }
}

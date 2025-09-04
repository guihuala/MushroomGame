using UnityEngine;

public static class DebugManager
{
    public static bool ShowDebugLogs = true;
    
    public static void Log(string message, UnityEngine.Object context = null)
    {
        if (ShowDebugLogs)
            Debug.Log($"[FactoryDebug] {message}", context);
    }
    
    public static void LogWarning(string message, UnityEngine.Object context = null)
    {
        if (ShowDebugLogs)
            Debug.LogWarning($"[FactoryDebug] {message}", context);
    }
    
    public static void LogError(string message, UnityEngine.Object context = null)
    {
        if (ShowDebugLogs)
            Debug.LogError($"[FactoryDebug] {message}", context);
    }
}
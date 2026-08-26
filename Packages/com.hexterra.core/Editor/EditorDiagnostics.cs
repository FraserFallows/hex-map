using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;

namespace HexTerra.Editor
{
    public static class EditorDiagnostics
    {
        /// <summary>
        /// Clears the console window in the Unity Editor.
        /// </summary>
        public static void ClearConsole()
        {
            var method = Assembly.GetAssembly(typeof(SceneView)).GetType("UnityEditor.LogEntries").GetMethod("Clear");
            if (method != null) method.Invoke(new object(), null);
        }

        /// <summary>
        /// Executes a list of methods and measures the time taken to execute each method.
        /// </summary>
        /// <param name="processName">Name of the process or operation.</param>
        /// <param name="actions">List of Action delegates representing methods to execute.</param>
        public static void StopWatchPro(string processName, List<Action> actions)
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (var action in actions)
                action();

            stopwatch.Stop();
            UnityEngine.Debug.Log($"{processName} took {(float)stopwatch.ElapsedMilliseconds / 1000} seconds.");
        }
    }
}

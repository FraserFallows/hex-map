using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;

namespace HexTerra.Editor
{
    public static class EditorDiagnostics
    {
        public static void ClearConsole()
        {
            var method = Assembly.GetAssembly(typeof(SceneView)).GetType("UnityEditor.LogEntries").GetMethod("Clear");
            if (method != null) method.Invoke(new object(), null);
        }

        /// <summary>
        /// Runs the actions in order and logs the total elapsed time under processName.
        /// </summary>
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

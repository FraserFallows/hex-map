using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(HexMap))]
    public class HexMapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var hexMap = (HexMap)target;

            if (GUILayout.Button("Generate Map"))
            {
                EditorDiagnostics.StopWatchPro("Map generation", new List<Action> { EditorDiagnostics.ClearConsole, hexMap.BeginGeneration });
            }

            if (GUILayout.Button("Clear Map"))
            {
                EditorDiagnostics.StopWatchPro("Clearing map", new List<Action> { EditorDiagnostics.ClearConsole, hexMap.ClearMap });
            }
        }
    }
}

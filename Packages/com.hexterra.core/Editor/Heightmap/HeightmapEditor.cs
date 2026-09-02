using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(Heightmap))]
    public class HeightmapEditor : UnityEditor.Editor
    {
        private Texture2D _preview;

        private void OnDisable()
        {
            if (_preview) DestroyImmediate(_preview);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("noise"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("noiseScale"));

            serializedObject.ApplyModifiedProperties();

            var heightmap = (Heightmap)target;
            NoisePreview.Render(ref _preview, heightmap.noise, heightmap.noiseScale,
                EditorGUI.EndChangeCheck() || !_preview, steps: Mathf.Max(1, heightmap.maxHeight));
            NoisePreview.DrawTextures(_preview);
        }
    }
}

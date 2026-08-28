using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(NoisePreset))]
    public class NoisePresetEditor : UnityEditor.Editor
    {
        private const int PreviewResolution = 256;
        private const float PreviewHexSpan = 64f;

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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bands"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("noiseScale"));

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck() || !_preview)
                Rebuild((NoisePreset)target);

            if (!_preview)
                return;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var rect = GUILayoutUtility.GetRect(PreviewResolution, PreviewResolution, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, _preview, null, ScaleMode.ScaleToFit);
            }
        }

        private void Rebuild(NoisePreset preset)
        {
            if (preset.noise == null)
                return;

            if (!_preview)
                _preview = new Texture2D(PreviewResolution, PreviewResolution) { hideFlags = HideFlags.DontSave };

            // A fixed window of hexes, so presets are comparable at a glance.
            var span = PreviewHexSpan / Mathf.Max(preset.noiseScale, 0.0001f);
            HeightmapConverter.Render(_preview, preset.noise, span, Vector2.zero, bands: Mathf.Max(1, preset.bands));
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(SurfacePaletteSet))]
    public class SurfacePaletteSetEditor : UnityEditor.Editor
    {
        private const string LutProperty = "_TintLUT";
        private const string DirtWallStepsProperty = "_DirtWallSteps";
        private const string RockWallStepsProperty = "_RockWallSteps";

        private Material _target;
        private Texture2D _preview;

        private void OnDisable()
        {
            if (_preview) DestroyImmediate(_preview);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("grass"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dirt"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rock"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dirtWallSteps"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rockWallSteps"));

            serializedObject.ApplyModifiedProperties();

            var palette = (SurfacePaletteSet)target;
            if (EditorGUI.EndChangeCheck() || !_preview)
            {
                _preview = palette.BakeLut(_preview);
                _preview.hideFlags = HideFlags.DontSave;
            }

            DrawPreview();

            EditorGUILayout.Space();
            _target = (Material)EditorGUILayout.ObjectField("Apply To Material", _target, typeof(Material), false);

            var assetPath = AssetDatabase.GetAssetPath(palette);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorGUILayout.HelpBox("Save this asset to bake its lookup texture.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Bake LUT"))
                BakeAndSave(palette, assetPath);
        }

        private void DrawPreview()
        {
            if (!_preview) return;

            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawPreviewTexture(rect, _preview, null, ScaleMode.StretchToFill);
        }

        // Bakes into the sibling "<name> LUT.asset", creating it on first run, then stamps the
        // optional target material so it picks up the new colours without a scene regenerate.
        private void BakeAndSave(SurfacePaletteSet palette, string assetPath)
        {
            var lutPath = Path.Combine(Path.GetDirectoryName(assetPath) ?? "", $"{palette.name} LUT.asset")
                .Replace('\\', '/');

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(lutPath);
            var lut = palette.BakeLut(existing);

            if (lut == existing)
                EditorUtility.SetDirty(lut);
            else
                AssetDatabase.CreateAsset(lut, lutPath);

            if (_target)
            {
                _target.SetTexture(LutProperty, lut);
                _target.SetFloat(DirtWallStepsProperty, palette.dirtWallSteps);
                _target.SetFloat(RockWallStepsProperty, palette.rockWallSteps);
                EditorUtility.SetDirty(_target);
            }

            AssetDatabase.SaveAssets();
        }
    }
}

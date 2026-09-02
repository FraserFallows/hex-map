using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(Surfacemap))]
    public class SurfacemapEditor : UnityEditor.Editor
    {
        private Texture2D _noisePreview;
        private Texture2D _tintPreview;
        private Texture2D _kindMap;
        private Texture2D _shadedMap;

        private SurfacemapPreview.Classification _classification;
        private bool _classStale;
        private bool _kindStale;
        private bool _shadedStale;

        private void OnDisable()
        {
            Discard(ref _noisePreview);
            Discard(ref _tintPreview);
            Discard(ref _kindMap);
            Discard(ref _shadedMap);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var surfacemap = (Surfacemap)target;
            var context = SurfacemapPreview.Context;

            // A drag or text edit in progress: keep the last recipe maps and rebuild once it
            // settles, rather than reclassifying 256x256 texels every repaint.
            bool settled = GUIUtility.hotControl == 0 && !EditorGUIUtility.editingTextField;

            EditorGUILayout.HelpBox(
                "Slope, height, convexity and noise blend into a score per cell. Below Dirt Threshold "
                + "is Grass, between the two is Dirt, at Rock Threshold or above is Rock. Cleanup passes then "
                + "smooth it.",
                MessageType.None);

            // Each noise field's preview sits directly under its own settings, paired to its left
            // with the recipe run over the heightmap noise when an owning map supplies one.
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script", "tint");
            serializedObject.ApplyModifiedProperties();
            bool classifierChanged = EditorGUI.EndChangeCheck();

            NoisePreview.Render(ref _noisePreview, surfacemap.noise, surfacemap.noiseScale,
                classifierChanged || !_noisePreview);

            // One classify feeds both maps; painting each from it is cheap by comparison.
            _classStale |= classifierChanged || context.dirty;
            bool reclassified = false;
            if (!context.valid)
            {
                _classification = null;
            }
            else if (_classification == null || (_classStale && settled))
            {
                _classification = SurfacemapPreview.Classify(_classification, context.heightmap,
                    surfacemap, context.palette, context.seed);
                _classStale = false;
                reclassified = _classification != null;
            }

            _kindStale |= reclassified;
            UpdateMap(ref _kindMap, ref _kindStale, settled, context.valid, surfacemap, context, shaded: false);
            NoisePreview.DrawTextures(_kindMap, _noisePreview);

            EditorGUILayout.LabelField("Vertex Tint", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tint.perCell"),
                new GUIContent("Per-Cell Tint",
                    "Sample the tint once per cell (flat colour, hard edges) instead of per vertex (smooth drift)."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tint.noise"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tint.noiseScale"));
            serializedObject.ApplyModifiedProperties();
            bool tintChanged = EditorGUI.EndChangeCheck();

            NoisePreview.Render(ref _tintPreview, surfacemap.tint.noise, surfacemap.tint.noiseScale,
                tintChanged || !_tintPreview);

            _shadedStale |= reclassified || tintChanged;
            UpdateMap(ref _shadedMap, ref _shadedStale, settled, context.valid && context.palette,
                surfacemap, context, shaded: true);
            NoisePreview.DrawTextures(_shadedMap, _tintPreview);
        }

        // Repaints the map from the shared classification when stale and nothing is being dragged,
        // or when it has no texture yet; clears it when it cannot render (no owner, or no palette
        // for the shaded map).
        private void UpdateMap(ref Texture2D cache, ref bool stale, bool settled, bool renderable,
            Surfacemap surfacemap, SurfacemapPreview.RecipeContext context, bool shaded)
        {
            if (!renderable || _classification == null)
            {
                Discard(ref cache);
                stale = false;
                return;
            }

            if (!cache || (stale && settled))
            {
                cache = SurfacemapPreview.Paint(_classification, cache, surfacemap, context.palette,
                    context.seed, shaded);
                stale = false;
            }
        }

        private static void Discard(ref Texture2D texture)
        {
            if (texture)
                DestroyImmediate(texture);
            texture = null;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(HexMap))]
    public class HexMapEditor : UnityEditor.Editor
    {
        private PresetPanel<Heightmap> _heightmapPanel;
        private PresetPanel<Surfacemap> _surfacemapPanel;

        private Texture2D _resultMap;
        private int _resultMapGeneration = -1;

        // What the result map reads, set end-of-pass and consumed on the next repaint.
        private bool _resultDirty;

        // The subset of _resultDirty the classification tracks: seed, heightmap, palette (not tint or size).
        private bool _classifyDirty;

        private bool _resultStale;

        private void OnEnable()
        {
            _heightmapPanel = new PresetPanel<Heightmap>(
                () => target ? ((HexMap)target).heightmapOverride : null,
                v => { if (target) ((HexMap)target).heightmapOverride = v; },
                Regenerate,
                "heightmap", "Packages/com.hexterra.core/Data/Heightmaps", "Heightmap");

            _surfacemapPanel = new PresetPanel<Surfacemap>(
                () => target ? ((HexMap)target).surfacemapOverride : null,
                v => { if (target) ((HexMap)target).surfacemapOverride = v; },
                Regenerate,
                "surfacemap", "Packages/com.hexterra.core/Data/Surfacemaps", "Surfacemap");

            _heightmapPanel.OnEnable(serializedObject);
            _surfacemapPanel.OnEnable(serializedObject);
        }

        private void OnDisable()
        {
            _heightmapPanel?.OnDisable();
            _surfacemapPanel?.OnDisable();
            Discard(ref _resultMap);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var map = (HexMap)target;
            var kind = (HeightmapSourceKind)serializedObject.FindProperty("source").enumValueIndex;

            bool layoutChanged = DrawLayoutZone(map, out bool seedChanged);
            bool heightmapChanged = DrawHeightmapZone();
            bool surfaceChanged = DrawSurfacemapZone(out bool paletteChanged);
            DrawRenderingZone();
            DrawEventZone();

            serializedObject.ApplyModifiedProperties();

            DrawActions(map, kind);

            _resultDirty = layoutChanged | heightmapChanged | surfaceChanged;
            _classifyDirty = seedChanged | heightmapChanged | paletteChanged;
        }

        private void Regenerate()
        {
            if (target is HexMap map && map && map.CanGenerate)
                map.BeginGeneration();
        }

        // Boxed section with a bold header. Use in a using statement; the body draws inside the box.
        private static EditorGUILayout.VerticalScope Zone(string title)
        {
            var scope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            return scope;
        }

        // Shape, size and seed, ending with the result map: the last build's cells through the
        // palette gradient, so the top of the inspector shows what the current config produces.
        // seedChanged is split out because the recipe classification tracks the seed but not size.
        private bool DrawLayoutZone(HexMap map, out bool seedChanged)
        {
            using (Zone("Map"))
            {
                EditorGUI.BeginChangeCheck();

                var shapeProp = serializedObject.FindProperty("shape");
                EditorGUILayout.PropertyField(shapeProp);

                if ((MapShape)shapeProp.enumValueIndex == MapShape.Hexagon)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("width"),
                        new GUIContent("Width", "Hexes across the middle, rounded up to an odd count"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
                }

                EditorGUI.BeginChangeCheck();
                var seedProp = serializedObject.FindProperty("seed");
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(seedProp);
                    if (GUILayout.Button("Randomise", GUILayout.Width(90)))
                        seedProp.intValue = Random.Range(0, int.MaxValue);
                }
                seedChanged = EditorGUI.EndChangeCheck();

                bool layoutChanged = EditorGUI.EndChangeCheck();

                DrawResultMap(map, _resultDirty || layoutChanged);
                return layoutChanged;
            }
        }

        private bool DrawHeightmapZone()
        {
            using (Zone("Heightmap"))
            {
                EditorGUI.BeginChangeCheck();

                var sourceProp = serializedObject.FindProperty("source");
                EditorGUILayout.PropertyField(sourceProp);

                bool embeddedChanged = false;
                EditorGUILayout.Space();
                switch ((HeightmapSourceKind)sourceProp.enumValueIndex)
                {
                    case HeightmapSourceKind.Noise:
                        embeddedChanged = _heightmapPanel.Draw(serializedObject);
                        break;
                    case HeightmapSourceKind.Texture:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("heightmapImage"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("textureBands"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("bilinear"));
                        break;
                    case HeightmapSourceKind.Flat:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("flatHeight"));
                        break;
                }

                return EditorGUI.EndChangeCheck() || embeddedChanged;
            }
        }

        // The palette that colours the result map, then the Surfacemap panel: its embedded editor
        // draws the classifier and tint noise previews, each paired with the recipe run live over
        // the heightmap noise. Returns whether anything the result or recipe maps read from changed.
        private bool DrawSurfacemapZone(out bool paletteChanged)
        {
            using (Zone("Surfacemap"))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("surfacePalette"));
                paletteChanged = EditorGUI.EndChangeCheck();

                EditorGUILayout.Space();

                // The embedded Surfacemap editor needs the owner's heightmap, palette and seed to
                // render the recipe maps; hand them over for the duration of its draw only.
                SurfacemapPreview.Context = new SurfacemapPreview.RecipeContext
                {
                    heightmap = _heightmapPanel.EnsureWorking(serializedObject),
                    palette = serializedObject.FindProperty("surfacePalette").objectReferenceValue as SurfacePaletteSet,
                    seed = serializedObject.FindProperty("seed").intValue,
                    dirty = _classifyDirty,
                    valid = true
                };
                bool embeddedChanged;
                try { embeddedChanged = _surfacemapPanel.Draw(serializedObject); }
                finally { SurfacemapPreview.Context = default; }

                return paletteChanged || embeddedChanged;
            }
        }

        private void DrawRenderingZone()
        {
            using (Zone("Rendering"))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hexTopPrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hexWallPrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hexSurfaceMaterial"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hexEdgeMaterial"));
            }
        }

        private void DrawEventZone()
        {
            using (Zone("Events"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mapGenerated"), true);
        }

        private void DrawActions(HexMap map, HeightmapSourceKind kind)
        {
            EditorGUILayout.Space();

            if (!map.CanGenerate)
            {
                EditorGUILayout.HelpBox(
                    kind == HeightmapSourceKind.Noise
                        ? "Assign a Heightmap to generate."
                        : "Assign a heightmap image to generate.",
                    MessageType.Info);
                return;
            }

            HexMapAutoRegenerate.Enabled =
                EditorGUILayout.ToggleLeft("Auto-regenerate on change", HexMapAutoRegenerate.Enabled);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate"))
                    EditorDiagnostics.StopWatchPro("Map generation", map.BeginGeneration);

                if (GUILayout.Button("Clear"))
                    EditorDiagnostics.StopWatchPro("Clearing map", map.ClearMap);
            }

            if (GUILayout.Button("Randomise Seed & Generate"))
            {
                serializedObject.FindProperty("seed").intValue = Random.Range(0, int.MaxValue);
                serializedObject.ApplyModifiedProperties();
                map.BeginGeneration();
            }
        }

        // The last build's cells through the palette gradient, shaded by height: the map's
        // finished look. Follows Generation, and tint / palette / seed edits settle in once no
        // control is being dragged. Drawn only with a palette assigned.
        private void DrawResultMap(HexMap map, bool rebuild)
        {
            var palette = serializedObject.FindProperty("surfacePalette").objectReferenceValue as SurfacePaletteSet;
            if (!palette)
            {
                Discard(ref _resultMap);
                _resultStale = false;
                return;
            }

            _resultStale |= rebuild;
            bool settled = GUIUtility.hotControl == 0 && !EditorGUIUtility.editingTextField;

            if (!_resultMap || map.Generation != _resultMapGeneration || (_resultStale && settled))
            {
                var recipe = _surfacemapPanel.EnsureWorking(serializedObject);
                var tintNoise = recipe.tint.noise;
                float tintScale = SafeScale(recipe.tint.noiseScale);
                int seed = serializedObject.FindProperty("seed").intValue;

                _resultMap = SurfaceKindMap.Render(map.Cells, _resultMap, palette, tintNoise, tintScale, seed);
                _resultMapGeneration = map.Generation;
            }

            NoisePreview.DrawTextures(_resultMap);
        }

        private static float SafeScale(float value) => Mathf.Max(value, 0.0001f);

        private static void Discard(ref Texture2D texture)
        {
            if (texture)
                DestroyImmediate(texture);
            texture = null;
        }
    }
}

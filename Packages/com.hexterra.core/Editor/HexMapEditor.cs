using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(HexMap))]
    public class HexMapEditor : UnityEditor.Editor
    {
        private const string PresetFolder = "Packages/com.hexterra.core/Data/Heightmap/Noise";

        private UnityEditor.Editor _presetEditor;

        // Noise is always edited on an in-memory working copy. Save writes it back to the asset
        // (or creates one); deselecting the HexMap drops the copy so the asset reloads untouched.
        private NoisePreset _working;
        private NoisePreset _sourceAsset;
        private bool _workingDirty;
        private bool _saveQueued;

        private void OnDisable()
        {
            if (_presetEditor != null)
                DestroyImmediate(_presetEditor);

            // Deselecting with unsaved edits: drop them and put the scene map back to the saved asset.
            var revert = _workingDirty;
            var map = target as HexMap;
            DiscardWorking();
            _sourceAsset = null;

            if (revert && map && map.CanGenerate
                && !EditorApplication.isCompiling
                && !EditorApplication.isPlayingOrWillChangePlaymode)
                map.BeginGeneration();
        }

        public override void OnInspectorGUI()
        {
            if (_saveQueued)
            {
                _saveQueued = false;
                SaveWorking();
            }

            serializedObject.Update();

            var shapeProp = serializedObject.FindProperty("shape");
            EditorGUILayout.PropertyField(shapeProp);

            if ((MapShape)shapeProp.enumValueIndex == MapShape.Hexagon)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("width"),
                    new GUIContent("Radius", "Hexes from centre to edge — 2 spans 3 across"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));

            var sourceProp = serializedObject.FindProperty("source");
            EditorGUILayout.PropertyField(sourceProp);
            var kind = (HeightmapSourceKind)sourceProp.enumValueIndex;

            EditorGUILayout.Space();
            switch (kind)
            {
                case HeightmapSourceKind.Noise:
                    DrawNoiseSource();
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

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hexTopPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hexWallPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hexTopMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hexWallMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hexEdgeMaterial"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mapGenerated"), true);

            serializedObject.ApplyModifiedProperties();

            var hexMap = (HexMap)target;

            EditorGUILayout.Space();
            if (!hexMap.CanGenerate)
            {
                EditorGUILayout.HelpBox(
                    kind == HeightmapSourceKind.Noise
                        ? "Assign a Noise Preset to generate."
                        : "Assign a heightmap image to generate.",
                    MessageType.Info);
                return;
            }

            HexMapAutoRegenerate.Enabled = EditorGUILayout.ToggleLeft("Auto-regenerate on change", HexMapAutoRegenerate.Enabled);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate"))
                    EditorDiagnostics.StopWatchPro("Map generation", new List<Action> { EditorDiagnostics.ClearConsole, hexMap.BeginGeneration });

                if (GUILayout.Button("Clear"))
                    EditorDiagnostics.StopWatchPro("Clearing map", new List<Action> { EditorDiagnostics.ClearConsole, hexMap.ClearMap });
            }

            if (GUILayout.Button("Randomise Seed & Generate"))
            {
                serializedObject.FindProperty("seed").intValue = UnityEngine.Random.Range(0, int.MaxValue);
                serializedObject.ApplyModifiedProperties();
                hexMap.BeginGeneration();
            }
        }

        // The noise fields edit an in-memory copy that reaches the asset only on Save. Deselecting
        // the HexMap discards the copy, so the preset reloads from disk untouched.
        private void DrawNoiseSource()
        {
            var hexMap = (HexMap)target;
            var presetProp = serializedObject.FindProperty("noisePreset");
            EditorGUILayout.PropertyField(presetProp);
            var assigned = presetProp.objectReferenceValue as NoisePreset;

            if (assigned != _sourceAsset)
            {
                DiscardWorking();
                _sourceAsset = assigned;
            }

            if (!_working)
            {
                _working = assigned ? Instantiate(assigned) : ScriptableObject.CreateInstance<NoisePreset>();
                _working.name = assigned ? assigned.name : "Unsaved Noise";
                _working.hideFlags = HideFlags.DontSave;
                _workingDirty = false;
            }
            hexMap.noisePresetOverride = _working;

            if (assigned)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("New"))
                        EditorApplication.delayCall += () => ClearPresetField(hexMap);
                    if (GUILayout.Button("Duplicate"))
                        EditorApplication.delayCall += () => DuplicatePreset(hexMap, assigned);
                    if (GUILayout.Button("Show in Project"))
                        EditorGUIUtility.PingObject(assigned);
                }
                EditorGUILayout.HelpBox("Edits preview on the map but reach the preset only on Save. Deselecting the HexMap discards them.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Unsaved noise. Save to keep it — deselecting the HexMap discards it.", MessageType.None);
            }

            DrawEmbedded(_working);

            if (GUILayout.Button(assigned ? "Save" : "Save Noise Preset…"))
                _saveQueued = true;
        }

        private void DrawEmbedded(NoisePreset preset)
        {
            CreateCachedEditor(preset, null, ref _presetEditor);

            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                _presetEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                _workingDirty = true;
                HexMapAutoRegenerate.Poke();
            }
        }

        // Runs from the top of OnInspectorGUI, before any layout — the save dialogue and asset
        // writes must not happen inside the IMGUI layout pass.
        private void SaveWorking()
        {
            if (target is not HexMap map || !map || !_working)
                return;

            if (_sourceAsset)
            {
                // CopySerialized brings the whole serialised state across (including the
                // [SerializeReference] noise tree); restore the asset's own identity after.
                var assetName = _sourceAsset.name;
                Undo.RecordObject(_sourceAsset, "Save Noise Preset");
                EditorUtility.CopySerialized(_working, _sourceAsset);
                _sourceAsset.name = assetName;
                _sourceAsset.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(_sourceAsset);
                AssetDatabase.SaveAssets();
                DiscardWorking();
                return;
            }

            var path = EditorUtility.SaveFilePanelInProject(
                "Save Noise Preset", "New NoisePreset", "asset",
                "Save this noise as a reusable preset.", PresetFolder);
            if (string.IsNullOrEmpty(path))
                return;

            map.noisePresetOverride = null;
            _working.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(_working, path);
            AssetDatabase.SaveAssets();

            var saved = _working;
            _working = null;
            _workingDirty = false;
            _sourceAsset = saved;

            var so = new SerializedObject(map);
            so.FindProperty("noisePreset").objectReferenceValue = saved;
            so.ApplyModifiedProperties();
        }

        private void DiscardWorking()
        {
            if (target is HexMap map && map)
                map.noisePresetOverride = null;
            if (_working)
                DestroyImmediate(_working);
            _working = null;
            _workingDirty = false;
        }

        private static void ClearPresetField(HexMap map)
        {
            if (!map)
                return;

            var so = new SerializedObject(map);
            so.FindProperty("noisePreset").objectReferenceValue = null;
            so.ApplyModifiedProperties();
        }

        // Deferred out of OnInspectorGUI: asset creation must not run inside the IMGUI layout pass.
        private static void DuplicatePreset(HexMap map, UnityEngine.Object preset)
        {
            var source = AssetDatabase.GetAssetPath(preset);
            var copy = AssetDatabase.GenerateUniqueAssetPath(source);
            if (!AssetDatabase.CopyAsset(source, copy))
                return;

            AssignPreset(map, AssetDatabase.LoadAssetAtPath<NoisePreset>(copy));
        }

        private static void AssignPreset(HexMap map, NoisePreset preset)
        {
            if (!map || !preset)
                return;

            var so = new SerializedObject(map);
            so.FindProperty("noisePreset").objectReferenceValue = preset;
            so.ApplyModifiedProperties();
        }
    }
}

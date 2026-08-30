using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace HexTerra.Editor
{
    [CustomEditor(typeof(HexMap))]
    public class HexMapEditor : UnityEditor.Editor
    {
        private enum PendingSave { None, Overwrite, AsNew }

        private const string PresetFolder = "Packages/com.hexterra.core/Data/Heightmap/Noise";

        private UnityEditor.Editor _presetEditor;

        // Noise is edited on an in-memory working copy held by the map (HexMap.noisePresetOverride)
        // so it survives the inspector being rebuilt (a regenerate does that). Overwrite writes it
        // to the assigned asset, Save as New creates one, Reset reloads it from the asset.
        private NoisePreset _working;
        private NoisePreset _sourceAsset;
        private bool _workingDirty;
        private PendingSave _pendingSave;

        private void OnEnable()
        {
            // A regenerate can rebuild this inspector mid-edit. Reattach to the working copy the
            // map kept so those edits aren't lost; assume it may be dirty since we can't tell.
            var map = target as HexMap;
            if (map && map.noisePresetOverride)
            {
                _working = map.noisePresetOverride;
                _sourceAsset = serializedObject.FindProperty("noisePreset").objectReferenceValue as NoisePreset;
                _workingDirty = true;
            }
        }

        private void OnDisable()
        {
            if (_presetEditor != null)
                DestroyImmediate(_presetEditor);
        }

        public override void OnInspectorGUI()
        {
            if (_pendingSave != PendingSave.None)
            {
                var pending = _pendingSave;
                _pendingSave = PendingSave.None;
                if (pending == PendingSave.Overwrite)
                    OverwritePreset();
                else
                    SavePresetAsNew();
            }

            serializedObject.Update();

            var shapeProp = serializedObject.FindProperty("shape");
            EditorGUILayout.PropertyField(shapeProp);

            if ((MapShape)shapeProp.enumValueIndex == MapShape.Hexagon)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("width"),
                    new GUIContent("Radius", "Hexes from centre to edge, so 2 spans 3 across"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
            }

            var seedProp = serializedObject.FindProperty("seed");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(seedProp);
                if (GUILayout.Button("Randomise", GUILayout.Width(90)))
                    seedProp.intValue = UnityEngine.Random.Range(0, int.MaxValue);
            }

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
                seedProp.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                serializedObject.ApplyModifiedProperties();
                hexMap.BeginGeneration();
            }
        }

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

            // The working copy lives on the map; only clone a fresh one when there isn't one.
            _working = hexMap.noisePresetOverride;
            if (!_working)
            {
                _working = assigned ? Instantiate(assigned) : ScriptableObject.CreateInstance<NoisePreset>();
                _working.name = assigned ? assigned.name : "Unsaved Noise";
                _working.hideFlags = HideFlags.DontSave;
                _workingDirty = false;
                hexMap.noisePresetOverride = _working;
            }

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
                EditorGUILayout.HelpBox("Edits preview on the map but reach the preset only on Overwrite. Reset reloads from the preset.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Unsaved noise. Save as New to keep it. A script or scene reload discards it.", MessageType.None);
            }

            DrawEmbedded(_working);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!assigned || !_workingDirty))
                    if (GUILayout.Button("Overwrite"))
                        _pendingSave = PendingSave.Overwrite;

                if (GUILayout.Button("Save as New…"))
                    _pendingSave = PendingSave.AsNew;

                using (new EditorGUI.DisabledScope(!assigned || !_workingDirty))
                    if (GUILayout.Button("Reset"))
                        EditorApplication.delayCall += () => ResetWorking(hexMap);
            }
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

        // OverwritePreset and SavePresetAsNew run from the top of OnInspectorGUI, before any
        // layout. The save dialogue and asset writes must not happen inside the IMGUI layout pass.
        private void OverwritePreset()
        {
            if (!_working || !_sourceAsset)
                return;

            // CopySerialized brings the whole serialised state across (including the
            // [SerializeReference] noise tree). Restore the asset's own identity after.
            var assetName = _sourceAsset.name;
            Undo.RecordObject(_sourceAsset, "Overwrite Noise Preset");
            EditorUtility.CopySerialized(_working, _sourceAsset);
            _sourceAsset.name = assetName;
            _sourceAsset.hideFlags = HideFlags.None;
            EditorUtility.SetDirty(_sourceAsset);
            AssetDatabase.SaveAssets();
            DiscardWorking();
        }

        private void SavePresetAsNew()
        {
            if (target is not HexMap map || !map || !_working)
                return;

            var path = EditorUtility.SaveFilePanelInProject(
                "Save Noise Preset", "New NoisePreset", "asset",
                "Save this noise as a reusable preset.", PresetFolder);
            if (string.IsNullOrEmpty(path))
                return;

            // _working is an unsaved in-memory instance, so it can become the asset directly.
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

        // Drop in-memory edits and rebuild from the saved preset. DiscardWorking clears the
        // override, so generation reads the asset; the working copy is remade on the next repaint.
        private void ResetWorking(HexMap map)
        {
            DiscardWorking();

            if (map && map.CanGenerate)
                map.BeginGeneration();
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

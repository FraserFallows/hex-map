using System;
using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    /// <summary>
    /// The working-copy lifecycle for a ScriptableObject preset slot: an in-memory clone edited
    /// and previewed on the target before it is written to the asset, with New / Duplicate /
    /// Overwrite / Save as New / Reset. One instance per preset field on a custom editor.
    /// </summary>
    internal sealed class PresetPanel<T> where T : ScriptableObject
    {
        private readonly Func<T> _getOverride;
        private readonly Action<T> _setOverride;
        private readonly Action _regenerate;
        private readonly string _assetProperty;
        private readonly string _folder;
        private readonly string _noun;

        private UnityEditor.Editor _embedded;
        private T _working;
        private T _sourceAsset;
        private bool _workingDirty;

        public PresetPanel(Func<T> getOverride, Action<T> setOverride, Action regenerate,
            string assetProperty, string folder, string noun)
        {
            _getOverride = getOverride;
            _setOverride = setOverride;
            _regenerate = regenerate;
            _assetProperty = assetProperty;
            _folder = folder;
            _noun = noun;
        }

        // The live working copy: reattaches to the override the target kept, or clones the
        // assigned asset (or a fresh instance for an empty slot) and stores that. Non-null.
        // Callable before Draw so a section drawn earlier in the inspector can read it.
        public T EnsureWorking(SerializedObject owner)
        {
            _working = _getOverride();
            if (_working)
                return _working;

            var assigned = owner.FindProperty(_assetProperty).objectReferenceValue as T;
            _working = assigned ? UnityEngine.Object.Instantiate(assigned) : ScriptableObject.CreateInstance<T>();
            _working.name = assigned ? assigned.name : "Unsaved " + _noun;
            _working.hideFlags = HideFlags.DontSave;
            _workingDirty = false;
            _setOverride(_working);
            return _working;
        }

        // A regenerate can rebuild the host inspector mid-edit. Reattach to the working copy the
        // target kept; assume it may be dirty since we cannot tell.
        public void OnEnable(SerializedObject owner)
        {
            var kept = _getOverride();
            if (!kept)
                return;
            _working = kept;
            _sourceAsset = owner.FindProperty(_assetProperty).objectReferenceValue as T;
            _workingDirty = true;
        }

        public void OnDisable()
        {
            if (_embedded)
                UnityEngine.Object.DestroyImmediate(_embedded);
        }

        // Draws the asset picker, the working-copy buttons and the embedded preset inspector.
        // Returns true when an embedded edit happened this pass.
        public bool Draw(SerializedObject owner)
        {
            var host = owner.targetObject;
            var presetProp = owner.FindProperty(_assetProperty);
            EditorGUILayout.PropertyField(presetProp);
            var assigned = presetProp.objectReferenceValue as T;

            if (assigned != _sourceAsset)
            {
                Discard();
                _sourceAsset = assigned;
            }

            EnsureWorking(owner);

            if (assigned)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("New"))
                        EditorApplication.delayCall += () => SetAsset(host, null);
                    if (GUILayout.Button("Duplicate"))
                        EditorApplication.delayCall += () => Duplicate(host, assigned);
                    if (GUILayout.Button("Show in Project"))
                        EditorGUIUtility.PingObject(assigned);
                }
                EditorGUILayout.HelpBox("Edits preview on the map but reach the preset only on Overwrite. Reset reloads from the preset.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Unsaved. Save as New to keep it. A script or scene reload discards it.", MessageType.None);
            }

            bool changed = DrawEmbedded();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!assigned || !_workingDirty))
                    if (GUILayout.Button("Overwrite"))
                        EditorApplication.delayCall += Overwrite;

                if (GUILayout.Button("Save as New…"))
                    EditorApplication.delayCall += () => SaveAsNew(host);

                using (new EditorGUI.DisabledScope(!assigned || !_workingDirty))
                    if (GUILayout.Button("Reset"))
                        EditorApplication.delayCall += Reset;
            }

            return changed;
        }

        private bool DrawEmbedded()
        {
            UnityEditor.Editor.CreateCachedEditor(_working, null, ref _embedded);

            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                _embedded.OnInspectorGUI();
            if (!EditorGUI.EndChangeCheck())
                return false;

            _workingDirty = true;
            HexMapAutoRegenerate.Poke();
            return true;
        }

        // CopySerialized brings the whole serialised state across (including any [SerializeReference]
        // trees). Restore the asset's own identity after.
        private void Overwrite()
        {
            if (!_working || !_sourceAsset)
                return;

            var assetName = _sourceAsset.name;
            Undo.RecordObject(_sourceAsset, "Overwrite " + _noun);
            EditorUtility.CopySerialized(_working, _sourceAsset);
            _sourceAsset.name = assetName;
            _sourceAsset.hideFlags = HideFlags.None;
            EditorUtility.SetDirty(_sourceAsset);
            AssetDatabase.SaveAssets();
            Discard();
        }

        private void SaveAsNew(UnityEngine.Object host)
        {
            if (!_working)
                return;

            var path = EditorUtility.SaveFilePanelInProject(
                "Save " + _noun, "New " + _noun.Replace(" ", ""), "asset",
                "Save this as a reusable preset.", _folder);
            if (string.IsNullOrEmpty(path))
                return;

            // _working is an unsaved in-memory instance, so it can become the asset directly.
            _setOverride(null);
            _working.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(_working, path);
            AssetDatabase.SaveAssets();

            var saved = _working;
            _working = null;
            _workingDirty = false;
            _sourceAsset = saved;
            SetAsset(host, saved);
        }

        // Drop in-memory edits and rebuild from the saved preset. Discard clears the override, so
        // the next repaint remakes the working copy from the asset; the regenerate reflects it.
        private void Reset()
        {
            Discard();
            _regenerate();
        }

        private void Discard()
        {
            _setOverride(null);
            if (_working)
                UnityEngine.Object.DestroyImmediate(_working);
            _working = null;
            _workingDirty = false;
        }

        // Deferred out of OnInspectorGUI: asset creation must not run inside the IMGUI layout pass.
        private void Duplicate(UnityEngine.Object host, T preset)
        {
            var source = AssetDatabase.GetAssetPath(preset);
            var copy = AssetDatabase.GenerateUniqueAssetPath(source);
            if (AssetDatabase.CopyAsset(source, copy))
                SetAsset(host, AssetDatabase.LoadAssetAtPath<T>(copy));
        }

        private void SetAsset(UnityEngine.Object host, T value)
        {
            if (!host)
                return;

            var so = new SerializedObject(host);
            so.FindProperty(_assetProperty).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }
}

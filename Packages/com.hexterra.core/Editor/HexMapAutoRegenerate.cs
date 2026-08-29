using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    // Rebuilds every HexMap in the open scenes shortly after a HexMap or asset property
    // settles, while the toggle in the HexMap inspector is on. Waits for the field being
    // typed into to be committed, so a value is never rebuilt from mid-keystroke.
    [InitializeOnLoad]
    internal static class HexMapAutoRegenerate
    {
        private const string PrefKey = "HexTerra.AutoRegenerate";
        private const double Debounce = 0.2;  // seconds of quiet before rebuilding
        private const double Cooldown = 0.5;  // seconds after a rebuild to ignore its own churn

        internal static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, false);
            set
            {
                if (value != Enabled)
                    EditorPrefs.SetBool(PrefKey, value);
            }
        }

        private static bool _dirty;
        private static double _dirtiedAt;
        private static double _rebuiltAt = double.MinValue;

        // For edits that don't raise ObjectChangeEvents, e.g. tuning an unsaved in-memory preset.
        internal static void Poke()
        {
            if (!Enabled)
                return;

            _dirty = true;
            _dirtiedAt = EditorApplication.timeSinceStartup;
        }

        static HexMapAutoRegenerate()
        {
            ObjectChangeEvents.changesPublished += OnChanges;
            EditorApplication.update += OnUpdate;
        }

        private static void OnChanges(ref ObjectChangeEventStream stream)
        {
            if (!Enabled || EditorApplication.timeSinceStartup - _rebuiltAt < Cooldown)
                return;

            for (int i = 0; i < stream.length; i++)
            {
                var kind = stream.GetEventType(i);
                if (kind == ObjectChangeKind.ChangeGameObjectOrComponentProperties ||
                    kind == ObjectChangeKind.ChangeAssetObjectProperties)
                {
                    _dirty = true;
                    _dirtiedAt = EditorApplication.timeSinceStartup;
                    return;
                }
            }
        }

        private static void OnUpdate()
        {
            if (!_dirty || EditorApplication.timeSinceStartup - _dirtiedAt < Debounce)
                return;

            // Still typing into a field: hold until it loses focus, so the edit
            // regenerates on deselect rather than dropping the in-progress value.
            if (EditorGUIUtility.editingTextField)
                return;

            _dirty = false;
            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var map in Object.FindObjectsByType<HexMap>(FindObjectsSortMode.None))
                map.BeginGeneration();

            _rebuiltAt = EditorApplication.timeSinceStartup;
        }
    }
}

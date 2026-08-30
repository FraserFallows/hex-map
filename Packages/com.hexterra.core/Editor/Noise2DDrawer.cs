using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HexTerra.Editor
{
    /// <summary>
    /// Inspector type picker for a <see cref="Noise2D"/> reference field, with the selected
    /// instance's own fields drawn beneath it. Without it a field can only be edited as
    /// whatever subtype it was constructed with, and nested sources can't be swapped.
    /// </summary>
    [CustomPropertyDrawer(typeof(Noise2D), true)]
    public class Noise2DDrawer : PropertyDrawer
    {
        // One reorderable list per LayeredNoise.layers property path, rebound to a fresh property each repaint.
        private readonly Dictionary<string, ReorderableList> _layerLists = new();

        // Index 0 is the null entry; the rest are the concrete Noise2D types, primitives first.
        private static readonly Type[] Types = BuildTypes();
        private static readonly string[] Names =
            Types.Select(t => t == null ? "(None)" : ObjectNames.NicifyVariableName(t.Name)).ToArray();

        // Combiners that can be inserted above a node: they hold a `source` field, or are LayeredNoise.
        private static readonly Type[] InsertTargets = Types.Where(t => t != null && CanInsertAbove(t)).ToArray();

        // An insert/remove picked from the "…" menu, applied on the next OnGUI for that exact property.
        private static (UnityEngine.Object target, string path, Type insert, Noise2D removeTo)? _pendingReparent;

        // Inset of a node's fields from its container edge.
        private const float Pad = 4f;

        private static readonly GUIContent MenuIcon =
            new("…", "Insert a combiner above this node, or remove this node");

        private static GUIStyle _boldPopup;
        private static GUIStyle BoldPopup =>
            _boldPopup ??= new GUIStyle(EditorStyles.popup) { fontStyle = FontStyle.Bold };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (_pendingReparent is { } pending
                && pending.path == property.propertyPath
                && pending.target == property.serializedObject.targetObject)
            {
                _pendingReparent = null;
                Reparent(property, pending.insert, pending.removeTo);
                GUI.changed = true;
            }

            EditorGUI.BeginProperty(position, label, property);

            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var current = Mathf.Max(0, Array.IndexOf(Types, ResolveType(property)));

            if (property.managedReferenceValue == null)
            {
                var lr = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
                var vr = new Rect(lr.xMax + 2f, position.y, position.width - EditorGUIUtility.labelWidth - 2f, lineHeight);
                EditorGUI.LabelField(lr, label);
                EditorGUI.BeginChangeCheck();
                var pick = EditorGUI.Popup(vr, current, Names);
                if (EditorGUI.EndChangeCheck() && pick != current)
                    property.managedReferenceValue = Types[pick] == null ? null : Activator.CreateInstance(Types[pick]);
                EditorGUI.EndProperty();
                return;
            }

            // Each node draws inside its own bordered container; the bold type dropdown is its title.
            DrawContainer(position);

            var header = new Rect(position.x + Pad, position.y + Pad, position.width - 2f * Pad, lineHeight);
            var menuRect = new Rect(header.xMax - 24f, header.y, 24f, lineHeight);
            var foldW = Mathf.Clamp(EditorGUIUtility.labelWidth - Pad, 40f, header.width - 90f);
            var foldRect = new Rect(header.x, header.y, foldW, lineHeight);
            var typeRect = new Rect(foldRect.xMax + 2f, header.y, menuRect.x - foldRect.xMax - 4f, lineHeight);

            var expandable = Children(property).Any();
            if (expandable)
                property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);
            else
                EditorGUI.LabelField(foldRect, label);

            EditorGUI.BeginChangeCheck();
            var picked = EditorGUI.Popup(typeRect, current, Names, BoldPopup);
            if (EditorGUI.EndChangeCheck() && picked != current)
                property.managedReferenceValue = Types[picked] == null ? null : Activator.CreateInstance(Types[picked]);

            if (GUI.Button(menuRect, MenuIcon, EditorStyles.miniButton))
                ShowInsertMenu(property);

            if (expandable && property.isExpanded)
            {
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                var bodyX = position.x + Pad;
                var bodyW = position.width - 2f * Pad;
                var y = header.yMax + spacing;

                foreach (var child in Children(property))
                {
                    float h;
                    if (IsLayerList(child))
                    {
                        var list = GetLayerList(child);
                        h = list.GetHeight();
                        list.DoList(new Rect(bodyX, y, bodyW, h));
                    }
                    else
                    {
                        h = EditorGUI.GetPropertyHeight(child, true);
                        EditorGUI.PropertyField(new Rect(bodyX, y, bodyW, h), child, true);
                    }
                    y += h + spacing;
                }

                if (IsRemap(property) && GUI.Button(new Rect(bodyX, y, bodyW, lineHeight), "Calibrate to source range"))
                    CalibrateRemap(property);

                EditorGUI.indentLevel = indent;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
                return EditorGUI.GetPropertyHeight(property, label, true);

            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            if (property.managedReferenceValue == null)
                return lineHeight;

            var height = Pad + lineHeight;
            if (property.isExpanded)
            {
                height += spacing;
                foreach (var child in Children(property))
                    height += (IsLayerList(child) ? GetLayerList(child).GetHeight()
                                                  : EditorGUI.GetPropertyHeight(child, true))
                              + spacing;

                if (IsRemap(property))
                    height += lineHeight + spacing;
            }

            return height + Pad;
        }

        // managedReferenceFullTypename is "<assembly> <full type name>"; null or empty means no value.
        private static Type ResolveType(SerializedProperty property)
        {
            var parts = property.managedReferenceFullTypename.Split(' ');
            return parts.Length == 2 ? Types.FirstOrDefault(t => t != null && t.FullName == parts[1]) : null;
        }

        private static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                yield return iterator.Copy();
            }
        }

        // The one array field on any Noise2D: LayeredNoise.layers.
        private static bool IsLayerList(SerializedProperty property) =>
            property.propertyType == SerializedPropertyType.Generic && property.isArray
            && (property.arrayElementType == nameof(NoiseLayer) || property.name == "layers");

        private ReorderableList GetLayerList(SerializedProperty layers)
        {
            if (!_layerLists.TryGetValue(layers.propertyPath, out var list))
            {
                list = new ReorderableList(layers.serializedObject, layers, true, true, true, true);

                list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Layers");

                list.elementHeightCallback = index =>
                {
                    var element = list.serializedProperty.GetArrayElementAtIndex(index);
                    var lineHeight = EditorGUIUtility.singleLineHeight;
                    var gap = EditorGUIUtility.standardVerticalSpacing;
                    return gap
                        + RowHeight(element, "source") + gap
                        + lineHeight + gap
                        + lineHeight + gap
                        + RowHeight(element, "mask") + gap;
                };

                list.drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = list.serializedProperty.GetArrayElementAtIndex(index);
                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    var y = rect.y + EditorGUIUtility.standardVerticalSpacing;
                    DrawRow(rect, ref y, element.FindPropertyRelative("source"), "Source");
                    DrawRow(rect, ref y, element.FindPropertyRelative("blend"), "Blend");
                    DrawRow(rect, ref y, element.FindPropertyRelative("weight"), "Weight");
                    DrawRow(rect, ref y, element.FindPropertyRelative("mask"), "Mask");

                    EditorGUI.indentLevel = indent;
                };

                // Default add zero-inits the struct, leaving weight 0 (a silent no-op layer).
                list.onAddCallback = l =>
                {
                    var index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;

                    var element = l.serializedProperty.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("source").managedReferenceValue = new FractalNoise();
                    element.FindPropertyRelative("mask").managedReferenceValue = null;
                    element.FindPropertyRelative("blend").enumValueIndex = 0;
                    element.FindPropertyRelative("weight").floatValue = 1f;
                };

                _layerLists[layers.propertyPath] = list;
            }

            list.serializedProperty = layers;
            return list;
        }

        private static float RowHeight(SerializedProperty element, string relative) =>
            EditorGUI.GetPropertyHeight(element.FindPropertyRelative(relative), true);

        private static void DrawRow(Rect element, ref float y, SerializedProperty property, string label)
        {
            var height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(new Rect(element.x, y, element.width, height), property, new GUIContent(label), true);
            y += height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static bool IsRemap(SerializedProperty property) => ResolveType(property) == typeof(RemapNoise);

        // Sets inputMin/inputMax to the source's observed range over the preview window, so the remap
        // actually fills [0, 1] instead of the guessed default window doing almost nothing.
        private static void CalibrateRemap(SerializedProperty property)
        {
            var remap = property.managedReferenceValue as RemapNoise;
            if (remap?.source == null)
                return;

            var span = 64f;   // mirrors NoisePresetEditor.PreviewHexSpan
            if (property.serializedObject.targetObject is NoisePreset preset)
                span /= Mathf.Max(preset.noiseScale, 0.0001f);

            const int steps = 128;
            var min = float.MaxValue;
            var max = float.MinValue;
            for (int i = 0; i < steps; i++)
            for (int j = 0; j < steps; j++)
            {
                var v = remap.source.Sample(i / (float)steps * span, j / (float)steps * span);
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }

            property.FindPropertyRelative("inputMin").floatValue = min;
            property.FindPropertyRelative("inputMax").floatValue = max;
        }

        private static void ShowInsertMenu(SerializedProperty property)
        {
            var path = property.propertyPath;
            var target = property.serializedObject.targetObject;
            var inner = GetSource(property.managedReferenceValue);

            var menu = new GenericMenu();
            foreach (var candidate in InsertTargets)
            {
                var type = candidate;
                menu.AddItem(new GUIContent("Insert Above/" + ObjectNames.NicifyVariableName(type.Name)), false,
                    () => _pendingReparent = (target, path, type, null));
            }

            menu.AddSeparator("");
            if (inner != null)
                menu.AddItem(new GUIContent("Remove"), false, () => _pendingReparent = (target, path, null, inner));
            else
                menu.AddDisabledItem(new GUIContent("Remove"));

            menu.ShowAsContext();
        }

        // Inserts a fresh `insert` combiner above the current node (current becomes its source), or
        // removes the current node by replacing it with `removeTo`.
        private static void Reparent(SerializedProperty property, Type insert, Noise2D removeTo)
        {
            if (insert == null)
            {
                property.managedReferenceValue = removeTo;
                return;
            }

            var wrapper = Activator.CreateInstance(insert);
            SetSource(wrapper, property.managedReferenceValue as Noise2D);
            property.managedReferenceValue = wrapper;
        }

        private static bool CanInsertAbove(Type type) =>
            type == typeof(LayeredNoise)
            || (type.GetField("source", BindingFlags.Public | BindingFlags.Instance) is { } field
                && typeof(Noise2D).IsAssignableFrom(field.FieldType));

        // A wrapper's single upstream input: its `source`, or a LayeredNoise's first layer's source.
        private static Noise2D GetSource(object node)
        {
            if (node is LayeredNoise layered)
                return layered.layers is { Count: > 0 } ? layered.layers[0].source : null;
            return node?.GetType().GetField("source", BindingFlags.Public | BindingFlags.Instance)?.GetValue(node) as Noise2D;
        }

        private static void SetSource(object node, Noise2D value)
        {
            if (node is LayeredNoise layered)
                layered.layers.Add(new NoiseLayer { source = value, blend = LayerBlend.Add, weight = 1f });
            else
                node.GetType().GetField("source", BindingFlags.Public | BindingFlags.Instance)?.SetValue(node, value);
        }

        // Faint fill plus a 1px border. Nesting overdraws the fill, so deeper nodes read as darker.
        private static void DrawContainer(Rect rect)
        {
            var pro = EditorGUIUtility.isProSkin;
            EditorGUI.DrawRect(rect, pro ? new Color(1f, 1f, 1f, 0.03f) : new Color(0f, 0f, 0f, 0.025f));

            var border = pro ? new Color(0f, 0f, 0f, 0.4f) : new Color(0f, 0f, 0f, 0.15f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
        }

        private static Type[] BuildTypes()
        {
            var concrete = TypeCache.GetTypesDerivedFrom<Noise2D>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(Wraps)
                .ThenBy(t => t.Name);
            return new Type[] { null }.Concat(concrete).ToArray();
        }

        // A combiner transforms other Noise2Ds, held either as a direct reference or a layer list.
        private static bool Wraps(Type type) => type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(f => typeof(Noise2D).IsAssignableFrom(f.FieldType) || f.FieldType == typeof(List<NoiseLayer>));
    }
}

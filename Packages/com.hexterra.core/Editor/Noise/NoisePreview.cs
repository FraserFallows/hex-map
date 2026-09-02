using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    /// <summary>
    /// Right-aligned inspector previews. <see cref="Render"/> renders a <see cref="Noise2D"/> field
    /// into a cache; <see cref="DrawTextures"/> shows one or more already-built textures in a row.
    /// A lone preview draws at <see cref="Size"/> pixels with no label.
    /// </summary>
    internal static class NoisePreview
    {
        private const int Size = 256;
        private const float HexSpan = 64f;

        /// <summary>
        /// Re-renders <paramref name="noise"/> into <paramref name="cache"/> when
        /// <paramref name="rebuild"/> is set or it is empty. A positive <paramref name="steps"/>
        /// quantises the render into bands; a null field clears the cache.
        /// </summary>
        public static void Render(ref Texture2D cache, Noise2D noise, float scale, bool rebuild, int steps = 0)
        {
            if (noise == null)
            {
                if (cache)
                    Object.DestroyImmediate(cache);
                cache = null;
                return;
            }

            if (rebuild || !cache)
            {
                if (!cache)
                    cache = new Texture2D(Size, Size) { hideFlags = HideFlags.DontSave };
                HeightmapConverter.Render(cache, noise, HexSpan / Mathf.Max(scale, 0.0001f), Vector2.zero, steps);
            }
        }

        /// <summary>
        /// A right-aligned row of the non-null textures, in order. Side by side they shrink to
        /// share the inspector width; a lone texture draws at <see cref="Size"/>. Draws nothing
        /// when none are given.
        /// </summary>
        public static void DrawTextures(params Texture2D[] textures)
        {
            int count = 0;
            foreach (var texture in textures)
                if (texture)
                    count++;
            if (count == 0)
                return;
            
            float each = count > 1
                ? Mathf.Clamp((EditorGUIUtility.currentViewWidth - 56f - 6f * (count - 1)) / count, 100f, Size)
                : Size;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                bool first = true;
                foreach (var texture in textures)
                {
                    if (!texture)
                        continue;
                    if (!first)
                        GUILayout.Space(6f);
                    first = false;
                    var rect = GUILayoutUtility.GetRect(each, each, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit);
                }
            }
        }
    }
}

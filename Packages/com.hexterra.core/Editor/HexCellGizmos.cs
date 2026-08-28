using UnityEditor;
using UnityEngine;

namespace HexTerra.Editor
{
    internal static class HexCellGizmos
    {
        private const float MinLabelGapPixels = 24f;
        private const int MaxAxisWalk = 512; // backstop; the viewport check normally stops sooner

        // Neighbour indices in HexMath.Directions: 1/4 = ±q, 0/3 = ±r, 2/5 = the s-constant diagonal
        private const int QPlus = 1, QMinus = 4, RPlus = 0, RMinus = 3;
        private static readonly int[] SNeighbours = { 2, 5 };

        private static readonly Color QAxisColour = new(0.63f, 0.16f, 0.24f);
        private static readonly Color RAxisColour = new(0.24f, 0.44f, 0.78f);
        private static readonly Color BackdropColour = new(0f, 0f, 0f, 0.55f);
        private static readonly string QTag = "#" + ColorUtility.ToHtmlStringRGB(QAxisColour);
        private static readonly string RTag = "#" + ColorUtility.ToHtmlStringRGB(RAxisColour);

        private static GUIStyle _selectedStyle;
        private static GUIStyle _labelStyle;
        private static Texture2D _backdrop;

        // Labels a selected hex and the hexes along its q and r axes with the full q,r
        // coordinate — the axis component in its colour, everything else grey. Each axis runs
        // to the edge of the view, thinning labels so they stay MinLabelGapPixels apart.
        [DrawGizmo(GizmoType.Selected)]
        private static void DrawCoordinates(HexCell cell, GizmoType _)
        {
            EnsureStyles();

            var viewport = Viewport();

            Handles.BeginGUI();

            var originScreen = HandleUtility.WorldToGUIPoint(Anchor(cell));
            Label(originScreen, $"<color={QTag}>{cell.q}</color>, <color={RTag}>{cell.r}</color>", _selectedStyle);

            DrawAxis(cell, QPlus, originScreen, viewport, isQ: true);
            DrawAxis(cell, QMinus, originScreen, viewport, isQ: true);
            DrawAxis(cell, RPlus, originScreen, viewport, isQ: false);
            DrawAxis(cell, RMinus, originScreen, viewport, isQ: false);

            foreach (var i in SNeighbours)
                if (cell.neighbours[i] && cell.neighbours[i].TryGetComponent(out HexCell s))
                    Label(HandleUtility.WorldToGUIPoint(Anchor(s)), $"{s.q}, {s.r}", _labelStyle);

            Handles.EndGUI();
        }

        // Walks the neighbour chain from origin, drawing a label only when it clears
        // MinLabelGapPixels from the last one, and stopping once the axis leaves the view.
        private static void DrawAxis(HexCell origin, int dirIndex, Vector2 lastScreen, Rect viewport, bool isQ)
        {
            var current = origin;

            for (int i = 0; i < MaxAxisWalk; i++)
            {
                if (!current.neighbours[dirIndex] || !current.neighbours[dirIndex].TryGetComponent(out HexCell next))
                    return;
                current = next;

                var point = HandleUtility.WorldToGUIPointWithDepth(Anchor(current));
                if (point.z < 0f)
                    return; // behind the camera

                var screen = new Vector2(point.x, point.y);
                if (!viewport.Contains(screen))
                    return; // a straight axis that has left the view will not re-enter

                if (Vector2.Distance(screen, lastScreen) < MinLabelGapPixels)
                    continue;

                Label(screen, isQ ? QLabel(current) : RLabel(current), _labelStyle);
                lastScreen = screen;
            }
        }

        private static string QLabel(HexCell h) => $"<color={QTag}>{h.q}</color>, {h.r}";
        private static string RLabel(HexCell h) => $"{h.q}, <color={RTag}>{h.r}</color>";

        private static Vector3 Anchor(HexCell cell) => cell.transform.position + Vector3.up * 0.3f;

        private static Rect Viewport()
        {
            var camera = Camera.current;
            if (camera == null)
                return new Rect(-1000, -1000, 6000, 6000);

            var scale = EditorGUIUtility.pixelsPerPoint;
            var rect = new Rect(0, 0, camera.pixelWidth / scale, camera.pixelHeight / scale);
            rect.xMin -= 64;
            rect.yMin -= 64;
            rect.xMax += 64;
            rect.yMax += 64;
            return rect;
        }

        private static void Label(Vector2 screen, string text, GUIStyle style)
        {
            var content = new GUIContent(text);
            var size = style.CalcSize(content);
            GUI.Label(new Rect(screen.x - size.x / 2f, screen.y - size.y / 2f, size.x, size.y), content, style);
        }

        private static void EnsureStyles()
        {
            if (_backdrop == null)
            {
                _backdrop = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                _backdrop.SetPixel(0, 0, BackdropColour);
                _backdrop.Apply();
            }

            _selectedStyle ??= new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter, richText = true, fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 2, 2),
                normal = { textColor = Color.white, background = _backdrop }
            };
            _labelStyle ??= new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter, richText = true,
                padding = new RectOffset(5, 5, 2, 2),
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f), background = _backdrop }
            };
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HexTerra.Pathfinding.Editor
{
    internal static class PathfindingVisualiserGizmos
    {
        private const int MaxCostLabels = 40;

        private static readonly List<int> Path = new();
        private static readonly List<int> Costs = new();
        private static Vector3[] _points = Array.Empty<Vector3>();

        private static PathGraph _solvedGraph;
        private static Vector2Int _solvedStart;
        private static Vector2Int _solvedGoal;
        private static bool _bothOnMap;
        private static bool _found;

        private static readonly Color StartColour = new(0.30f, 0.80f, 0.35f);
        private static readonly Color GoalColour = new(0.25f, 0.65f, 0.95f);
        private static readonly Color BlockedColour = new(0.90f, 0.30f, 0.30f);

        // One colour per turn a hex is reached on; cycles past the last.
        private static readonly Color[] TurnColours =
        {
            new(0.35f, 0.85f, 0.40f),
            new(0.95f, 0.85f, 0.30f),
            new(0.95f, 0.55f, 0.20f),
            new(0.90f, 0.32f, 0.32f),
            new(0.72f, 0.42f, 0.90f),
        };

        private static GUIStyle _style;

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void Draw(PathfindingVisualiser vis, GizmoType _)
        {
            var graph = vis.TryGetComponent(out Pathfinder pathfinder) ? pathfinder.Graph : null;
            if (graph == null || graph.NodeCount == 0)
                return;

            Solve(pathfinder, graph, vis.start, vis.goal);

            int start = graph.IndexOf(vis.start);
            int goal = graph.IndexOf(vis.goal);
            bool normal = _found || !_bothOnMap;

            if (start >= 0) Marker(graph.WorldPositionOf(start), normal ? StartColour : BlockedColour);
            if (goal >= 0) Marker(graph.WorldPositionOf(goal), normal ? GoalColour : BlockedColour);

            if (!_bothOnMap)
                return;

            if (!_found)
            {
                var mid = (graph.WorldPositionOf(start) + graph.WorldPositionOf(goal)) * 0.5f;
                Handles.BeginGUI();
                GuiLabel(mid + Vector3.up * 0.4f, "no path");
                Handles.EndGUI();
                return;
            }

            int movePoints = pathfinder.MovePoints;
            DrawTurnBands(movePoints);

            Handles.BeginGUI();
            if (vis.drawStepCosts)
            {
                int stride = Mathf.Max(1, _points.Length / MaxCostLabels);
                for (int i = 0; i < _points.Length; i += stride)
                    GuiLabel(_points[i] + Vector3.up * 0.22f, Costs[i].ToString());
            }
            int turns = _turns[_points.Length - 1] + 1;
            GuiLabel(_points[^1] + Vector3.up * 0.5f,
                $"{Costs[^1]}  ({Path.Count - 1} steps, {turns} turn{(turns == 1 ? "" : "s")})");
            Handles.EndGUI();
        }

        // Re-solves only when the graph or an endpoint changed. Live edits to the traversal
        // bands are not picked up: nudge an endpoint to refresh.
        private static void Solve(Pathfinder pathfinder, PathGraph graph, Vector2Int start, Vector2Int goal)
        {
            if (ReferenceEquals(graph, _solvedGraph) && start == _solvedStart && goal == _solvedGoal)
                return;

            _solvedGraph = graph;
            _solvedStart = start;
            _solvedGoal = goal;

            _bothOnMap = graph.IndexOf(start) >= 0 && graph.IndexOf(goal) >= 0;
            _found = _bothOnMap && pathfinder.TryFindPath(start, goal, Path, Costs);
            if (!_found)
                return;

            if (_points.Length != Path.Count)
                _points = new Vector3[Path.Count];
            for (int i = 0; i < Path.Count; i++)
                _points[i] = graph.WorldPositionOf(Path[i]) + Vector3.up * 0.06f;
        }

        private static int[] _turns = Array.Empty<int>();

        // A turn starts with a fresh movePoints budget; the leftover is discarded when the next
        // move will not fit, so a wasted point can push a node into a later turn.
        private static void DrawTurnBands(int movePoints)
        {
            int n = _points.Length;

            if (_turns.Length < Mathf.Max(1, n))
                _turns = new int[Mathf.Max(1, n)];
            _turns[0] = 0;

            int turn = 0;
            int remaining = movePoints;
            for (int k = 1; k < n; k++)
            {
                int edge = Costs[k] - Costs[k - 1];
                if (movePoints > 0 && edge > remaining)
                {
                    turn++;
                    remaining = movePoints;
                }
                remaining -= edge;
                _turns[k] = turn;
            }

            for (int i = 0; i < n - 1;)
            {
                int t = _turns[i + 1];
                int j = i + 1;
                while (j < n - 1 && _turns[j + 1] == t)
                    j++;

                var run = new Vector3[j - i + 1];
                Array.Copy(_points, i, run, 0, run.Length);
                Handles.color = TurnColours[t % TurnColours.Length];
                Handles.DrawAAPolyLine(5f, run);

                i = j;
            }
        }

        private static void Marker(Vector3 centre, Color colour)
        {
            var pos = centre + Vector3.up * 0.05f;
            Handles.color = new Color(colour.r, colour.g, colour.b, 0.35f);
            Handles.DrawSolidDisc(pos, Vector3.up, 0.5f);
            Handles.color = colour;
            Handles.DrawWireDisc(pos, Vector3.up, 0.5f, 2f);
        }

        // Call inside a Handles.BeginGUI / EndGUI block.
        private static void GuiLabel(Vector3 world, string text)
        {
            var point = HandleUtility.WorldToGUIPointWithDepth(world);
            if (point.z < 0f)
                return;

            var content = new GUIContent(text);
            var size = Style.CalcSize(content);
            GUI.Label(new Rect(point.x - size.x * 0.5f, point.y - size.y * 0.5f, size.x, size.y), content, Style);
        }

        private static GUIStyle Style
        {
            get
            {
                if (_style != null)
                    return _style;

                var backdrop = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                backdrop.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
                backdrop.Apply();

                _style = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(4, 4, 1, 1),
                    normal = { textColor = Color.white, background = backdrop }
                };
                return _style;
            }
        }
    }
}

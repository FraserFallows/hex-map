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

        private static readonly List<int> ReachNodes = new();
        private static PathGraph _reachGraph;
        private static Vector2Int _reachStart;
        private static int _reachBudget = -1;
        private static bool[] _inReach = Array.Empty<bool>();

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

        private static readonly Color ReachBorderColour = new(0.25f, 0.90f, 0.85f);

        // The two corner offsets per hex edge. Flat-top, circumradius 1 (HexMath spacing);
        // edge k faces 90 - 60k degrees.
        private static readonly Vector3[] EdgeCorners = BuildEdgeCorners();

        private static GUIStyle _style;

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void Draw(PathfindingVisualiser vis, GizmoType _)
        {
            var graph = vis.TryGetComponent(out Pathfinder pathfinder) ? pathfinder.Graph : null;
            if (graph == null || graph.NodeCount == 0)
                return;

            int start = graph.IndexOf(vis.start);
            int goal = graph.IndexOf(vis.goal);

            if (vis.drawReachable && start >= 0)
            {
                SolveReachable(pathfinder, graph, vis.start, pathfinder.MovePoints);
                DrawReachableBorder(graph);
            }

            bool normal = true;
            if (vis.drawPath)
            {
                Solve(pathfinder, graph, vis.start, vis.goal);
                normal = _found || !_bothOnMap;
            }

            // Start disc shows for either query (it anchors the range outline); goal disc is route-only.
            if (start >= 0 && (vis.drawPath || vis.drawReachable))
                Marker(graph.WorldPositionOf(start), normal ? StartColour : BlockedColour);
            if (goal >= 0 && vis.drawPath)
                Marker(graph.WorldPositionOf(goal), normal ? GoalColour : BlockedColour);

            if (!vis.drawPath || !_bothOnMap)
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

        // Re-solves the reachable set on a graph, start, or budget change; band edits need a nudge.
        private static void SolveReachable(Pathfinder pathfinder, PathGraph graph, Vector2Int start, int budget)
        {
            if (ReferenceEquals(graph, _reachGraph) && start == _reachStart && budget == _reachBudget)
                return;

            _reachGraph = graph;
            _reachStart = start;
            _reachBudget = budget;
            pathfinder.TryFindReachable(start, budget, ReachNodes);
        }

        // Draws an edge wherever a reachable hex borders one that is off-map or out of range, so
        // the outline wraps the region and any interior pockets it cannot enter.
        private static void DrawReachableBorder(PathGraph graph)
        {
            int count = graph.NodeCount;
            if (_inReach.Length < count)
                _inReach = new bool[count];
            else
                Array.Clear(_inReach, 0, count);
            foreach (int node in ReachNodes)
                _inReach[node] = true;

            Handles.color = ReachBorderColour;
            foreach (int node in ReachNodes)
            {
                var centre = graph.WorldPositionOf(node) + Vector3.up * 0.03f;
                ReadOnlySpan<int> neighbours = graph.NeighboursOf(node);
                for (int k = 0; k < 6; k++)
                {
                    if (neighbours[k] >= 0 && _inReach[neighbours[k]])
                        continue;

                    var corner0 = centre + EdgeCorners[k * 2];
                    var corner1 = centre + EdgeCorners[k * 2 + 1];
                    Handles.DrawAAPolyLine(3f, corner0, corner1);

                    // Each corner is shared with the adjacent side (k - 1 for corner0, k + 1 for corner1).
                    StepRiser(graph, centre.y, corner0, neighbours[(k + 5) % 6]);
                    StepRiser(graph, centre.y, corner1, neighbours[(k + 1) % 6]);
                }
            }
        }

        // Draws a vertical from a border corner down to the lower reachable hex sharing it, so a
        // height step reads as a wall. Drawn once, from the higher hex.
        private static void StepRiser(PathGraph graph, float topY, Vector3 corner, int adjacent)
        {
            if (adjacent < 0 || !_inReach[adjacent])
                return;

            float adjY = graph.WorldPositionOf(adjacent).y + 0.03f;
            if (adjY >= topY)
                return;

            Handles.DrawAAPolyLine(3f, new Vector3(corner.x, adjY, corner.z), corner);
        }

        private static int[] _turns = Array.Empty<int>();

        // Colours the route by the turn each hex falls in. Edge costs pack into successive movePoints
        // budgets with each turn's leftover discarded, so a wasted point can delay a hex by a turn.
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

        private static Vector3[] BuildEdgeCorners()
        {
            var corners = new Vector3[12];
            for (int k = 0; k < 6; k++)
            {
                float facing = (90f - 60f * k) * Mathf.Deg2Rad;
                corners[k * 2] = Corner(facing + Mathf.PI / 6f);
                corners[k * 2 + 1] = Corner(facing - Mathf.PI / 6f);
            }
            return corners;

            static Vector3 Corner(float a) => new(Mathf.Cos(a), 0f, Mathf.Sin(a));
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

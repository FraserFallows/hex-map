using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HexTerra.Pathfinding.Editor
{
    [CustomEditor(typeof(PathfindingVisualiser))]
    internal sealed class PathfindingVisualiserEditor : UnityEditor.Editor
    {
        private static readonly List<int> Path = new();
        private static readonly List<int> Costs = new();

        // Brings the transient graph back on its own after a domain reload.
        private static Pathfinder EnsureGraph(PathfindingVisualiser vis)
        {
            var pathfinder = vis.GetComponent<Pathfinder>();
            if (!pathfinder || pathfinder.Graph is { NodeCount: > 0 })
                return pathfinder;

            var map = FindAnyObjectByType<HexMap>();
            if (map && map.Cells.Count > 0)
                pathfinder.RebuildGraph(map);

            return pathfinder;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var vis = (PathfindingVisualiser)target;
            var pathfinder = EnsureGraph(vis);
            var graph = pathfinder ? pathfinder.Graph : null;

            EditorGUILayout.Space();

            if (graph == null || graph.NodeCount == 0)
            {
                var map = FindAnyObjectByType<HexMap>();

                if (!map)
                    EditorGUILayout.HelpBox("No HexMap in the scene.", MessageType.Info);
                else if (!map.CanGenerate)
                    EditorGUILayout.HelpBox("The HexMap can't generate yet — assign its heightmap source.", MessageType.Info);
                else
                {
                    EditorGUILayout.HelpBox(
                        "No graph. Generate the map (Pathfinder wired to mapGenerated), or build it here.",
                        MessageType.Info);

                    if (GUILayout.Button("Build graph now") && pathfinder)
                    {
                        if (map.Cells.Count == 0)
                            map.BeginGeneration();
                        pathfinder.RebuildGraph(map);
                        SceneView.RepaintAll();
                    }
                }
                return;
            }

            if (graph.IndexOf(vis.start) < 0 || graph.IndexOf(vis.goal) < 0)
            {
                EditorGUILayout.HelpBox("Set the start and goal: click a hex in the scene view, shift-click for the goal.", MessageType.Info);
                return;
            }

            if (pathfinder.TryFindPath(vis.start, vis.goal, Path, Costs))
                EditorGUILayout.LabelField($"Path: {Path.Count - 1} steps, cost {Costs[^1]}");
            else
                EditorGUILayout.HelpBox("No route between the chosen hexes (blocked by terrain steeper than the traversal bands allow).", MessageType.Warning);
        }

        private void OnSceneGUI()
        {
            var vis = (PathfindingVisualiser)target;
            var pathfinder = EnsureGraph(vis);
            var graph = pathfinder ? pathfinder.Graph : null;
            if (graph == null || graph.NodeCount == 0)
                return;

            // Claim the default control so a plain click reaches the handler, not a deselect.
            if (Event.current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Handles.BeginGUI();
            GUI.Label(new Rect(8, 8, 360, 18), "Click: set start   ·   Shift-click: set goal");
            Handles.EndGUI();

            EndpointHandle(vis, graph, ref vis.start);
            EndpointHandle(vis, graph, ref vis.goal);

            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || e.alt)
                return;

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out var hit))
                return;

            int node = NearestNode(graph, hit.point);
            if (node < 0)
                return;

            Undo.RecordObject(vis, "Set path endpoint");
            var coord = graph.CoordOf(node);
            if (e.shift) vis.goal = coord;
            else vis.start = coord;
            EditorUtility.SetDirty(vis);
            e.Use();
        }

        private static void EndpointHandle(PathfindingVisualiser vis, PathGraph graph, ref Vector2Int coord)
        {
            int node = graph.IndexOf(coord);
            if (node < 0)
                return;

            var pos = graph.WorldPositionOf(node) + Vector3.up * 0.1f;
            float size = HandleUtility.GetHandleSize(pos) * 0.22f;

            EditorGUI.BeginChangeCheck();
            Handles.FreeMoveHandle(pos, size, Vector3.zero, Handles.SphereHandleCap);
            if (!EditorGUI.EndChangeCheck())
                return;

            // Snap to the hex under the cursor, not to the handle's free-drag position.
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!Physics.Raycast(ray, out var hit))
                return;

            int nearest = NearestNode(graph, hit.point);
            if (nearest < 0 || nearest == node)
                return;

            Undo.RecordObject(vis, "Move path endpoint");
            coord = graph.CoordOf(nearest);
            EditorUtility.SetDirty(vis);
        }

        private static int NearestNode(PathGraph graph, Vector3 world)
        {
            int best = -1;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < graph.NodeCount; i++)
            {
                float sqr = (graph.WorldPositionOf(i) - world).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                
                bestSqr = sqr;
                best = i;
            }
            return best;
        }
    }
}

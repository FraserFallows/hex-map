using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HexTerra.Pathfinding.Tests
{
    public sealed class HexAStarTests
    {
        // baseCost 2; climb at most +2 steps (then a wall), drop at most -3.
        private static TraversalModel Rules() => new(2, new[] { 0, 1, 4 }, new[] { 0, 1, 2, 3 });

        [Test]
        public void StartEqualsGoalGivesASingleNodeAtZeroCost()
        {
            var graph = PathGraphFactory.FlatBlock(3, 3);
            var astar = new HexAStar(graph);
            var path = new List<int>();
            var costs = new List<int>();

            Assert.IsTrue(astar.TryFindPath(Rules(), 4, 4, path, costs));
            CollectionAssert.AreEqual(new[] { 4 }, path);
            CollectionAssert.AreEqual(new[] { 0 }, costs);
        }

        [Test]
        public void FlatLineIsWalkedDirectly()
        {
            var graph = PathGraphFactory.FromCells((0, 0, 0), (1, 0, 0), (2, 0, 0), (3, 0, 0), (4, 0, 0));
            var astar = new HexAStar(graph);
            var path = new List<int>();
            var costs = new List<int>();

            int start = graph.IndexOf(new Vector2Int(0, 0));
            int goal = graph.IndexOf(new Vector2Int(4, 0));

            Assert.IsTrue(astar.TryFindPath(Rules(), start, goal, path, costs));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, path);
            CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8 }, costs);
        }

        [Test]
        public void ACheaperDetourBeatsTheClimb()
        {
            // Direct (0,0)->(1,0)->(2,0) crosses a +2 tower (cost 6 + 4); the flat detour costs 6.
            var graph = PathGraphFactory.FromCells(
                (0, 0, 0), (1, 0, 2), (2, 0, 0), (1, -1, 0), (2, -1, 0));
            var astar = new HexAStar(graph);
            var path = new List<int>();
            var costs = new List<int>();

            int start = graph.IndexOf(new Vector2Int(0, 0));
            int goal = graph.IndexOf(new Vector2Int(2, 0));
            int tower = graph.IndexOf(new Vector2Int(1, 0));

            Assert.IsTrue(astar.TryFindPath(Rules(), start, goal, path, costs));
            CollectionAssert.DoesNotContain(path, tower);
            Assert.AreEqual(6, costs[^1]);
        }

        [Test]
        public void NoRouteReturnsFalseAndClearsTheOutputs()
        {
            // (1,0) is a +3 wall; Rules() climbs at most +2, and nothing else bridges the ends.
            var graph = PathGraphFactory.FromCells((0, 0, 0), (1, 0, 3), (2, 0, 0));
            var astar = new HexAStar(graph);
            var path = new List<int> { 99 };
            var costs = new List<int> { 99 };

            Assert.IsFalse(astar.TryFindPath(Rules(),
                graph.IndexOf(new Vector2Int(0, 0)), graph.IndexOf(new Vector2Int(2, 0)), path, costs));
            Assert.IsEmpty(path);
            Assert.IsEmpty(costs);
        }

        [Test]
        public void OutOfRangeEndpointsReturnFalse()
        {
            var graph = PathGraphFactory.FlatBlock(2, 2);
            var astar = new HexAStar(graph);
            var path = new List<int>();

            Assert.IsFalse(astar.TryFindPath(Rules(), -1, 0, path));
            Assert.IsFalse(astar.TryFindPath(Rules(), 0, graph.NodeCount, path));
        }

        [Test]
        public void CostsAreCumulativeAndThePathIsContiguous()
        {
            var rules = Rules();
            var graph = PathGraphFactory.Block(6, 6, (q, r) => (q * 2 + r) % 4);
            var astar = new HexAStar(graph);
            var path = new List<int>();
            var costs = new List<int>();

            int[] oracle = PathGraphFactory.ShortestCosts(graph, rules, 0);
            int goal = -1;
            for (int i = graph.NodeCount - 1; i > 0; i--)
                if (oracle[i] != int.MaxValue) { goal = i; break; }
            Assert.Greater(goal, 0);

            Assert.IsTrue(astar.TryFindPath(rules, 0, goal, path, costs));
            Assert.AreEqual(0, path[0]);
            Assert.AreEqual(goal, path[^1]);
            Assert.AreEqual(0, costs[0]);
            Assert.AreEqual(path.Count, costs.Count);

            for (int i = 1; i < path.Count; i++)
            {
                Assert.Contains(path[i], NeighboursOf(graph, path[i - 1]));
                int from = graph.StepHeightOf(path[i - 1]);
                int to = graph.StepHeightOf(path[i]);
                Assert.AreEqual(rules.MoveCost(from, to), costs[i] - costs[i - 1]);
            }
        }

        [Test]
        public void PathCostMatchesTheBruteForceOracle()
        {
            var rules = Rules();
            var path = new List<int>();
            var costs = new List<int>();

            foreach (int seed in new[] { 1, 7, 42, 1000 })
            {
                var rng = new System.Random(seed);
                var graph = PathGraphFactory.Block(6, 6, (_, _) => rng.Next(0, 4));
                var astar = new HexAStar(graph);

                for (int start = 0; start < graph.NodeCount; start += 5)
                {
                    int[] oracle = PathGraphFactory.ShortestCosts(graph, rules, start);
                    for (int goal = 0; goal < graph.NodeCount; goal += 3)
                    {
                        bool found = astar.TryFindPath(rules, start, goal, path, costs);
                        string where = $"seed {seed}, {start} -> {goal}";
                        if (oracle[goal] == int.MaxValue)
                            Assert.IsFalse(found, where);
                        else
                        {
                            Assert.IsTrue(found, where);
                            Assert.AreEqual(oracle[goal], costs[^1], where);
                        }
                    }
                }
            }
        }

        [Test]
        public void OneSolverServesRepeatedQueries()
        {
            var rules = Rules();
            var graph = PathGraphFactory.FromCells(
                (0, 0, 0), (1, 0, 2), (2, 0, 0), (1, -1, 0), (2, -1, 0));
            var astar = new HexAStar(graph);
            var path = new List<int>();
            var costs = new List<int>();

            int a = graph.IndexOf(new Vector2Int(0, 0));
            int b = graph.IndexOf(new Vector2Int(2, 0));
            int c = graph.IndexOf(new Vector2Int(1, -1));

            Assert.IsTrue(astar.TryFindPath(rules, a, b, path, costs));
            Assert.AreEqual(6, costs[^1]);

            Assert.IsTrue(astar.TryFindPath(rules, a, c, path, costs));
            CollectionAssert.AreEqual(new[] { a, c }, path);
            Assert.AreEqual(2, costs[^1]);

            Assert.IsTrue(astar.TryFindPath(rules, b, a, path, costs));
            Assert.AreEqual(6, costs[^1]);
        }

        private static int[] NeighboursOf(PathGraph graph, int node)
        {
            var span = graph.NeighboursOf(node);
            var arr = new int[span.Length];
            for (int i = 0; i < span.Length; i++)
                arr[i] = span[i];
            return arr;
        }
    }
}

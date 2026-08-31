using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HexTerra.Pathfinding.Tests
{
    public sealed class HexDijkstraTests
    {
        private static TraversalModel Rules() => new(2, new[] { 0, 1, 4 }, new[] { 0, 1, 2, 3 });

        [Test]
        public void ZeroBudgetReturnsOnlyTheStart()
        {
            var graph = PathGraphFactory.FlatBlock(3, 3);
            var dijkstra = new HexDijkstra(graph);
            var nodes = new List<int>();
            var costs = new List<int>();

            Assert.IsTrue(dijkstra.TryFindReachable(Rules(), 4, 0, nodes, costs));
            CollectionAssert.AreEqual(new[] { 4 }, nodes);
            CollectionAssert.AreEqual(new[] { 0 }, costs);
        }

        [Test]
        public void TheBudgetBoundaryIsInclusiveAndExact()
        {
            // Flat line, baseCost 2: costs from (0,0) are 0, 2, 4, 6, 8.
            var graph = PathGraphFactory.FromCells((0, 0, 0), (1, 0, 0), (2, 0, 0), (3, 0, 0), (4, 0, 0));
            var dijkstra = new HexDijkstra(graph);
            var nodes = new List<int>();
            var costs = new List<int>();

            dijkstra.TryFindReachable(Rules(), graph.IndexOf(new Vector2Int(0, 0)), 4, nodes, costs);

            CollectionAssert.Contains(nodes, graph.IndexOf(new Vector2Int(2, 0)));       // cost 4 == budget
            CollectionAssert.DoesNotContain(nodes, graph.IndexOf(new Vector2Int(3, 0))); // cost 6
            Assert.IsTrue(costs.All(c => c <= 4));
        }

        [Test]
        public void ResultsAreOrderedByAscendingCost()
        {
            var rng = new System.Random(3);
            var graph = PathGraphFactory.Block(6, 6, (_, _) => rng.Next(0, 4));
            var dijkstra = new HexDijkstra(graph);
            var nodes = new List<int>();
            var costs = new List<int>();

            dijkstra.TryFindReachable(Rules(), 0, 12, nodes, costs);

            Assert.AreEqual(0, nodes[0]);
            Assert.AreEqual(0, costs[0]);
            for (int i = 1; i < costs.Count; i++)
                Assert.GreaterOrEqual(costs[i], costs[i - 1]);
        }

        [Test]
        public void HexesBehindAnUnclimbableWallAreExcluded()
        {
            // (2,0) sits behind a +3 wall at (1,0); Rules() climbs at most +2.
            var graph = PathGraphFactory.FromCells((0, 0, 0), (1, 0, 3), (2, 0, 0));
            var dijkstra = new HexDijkstra(graph);
            var nodes = new List<int>();

            Assert.IsTrue(dijkstra.TryFindReachable(Rules(), graph.IndexOf(new Vector2Int(0, 0)), 100, nodes));
            CollectionAssert.AreEqual(new[] { graph.IndexOf(new Vector2Int(0, 0)) }, nodes);
        }

        [Test]
        public void OutOfRangeStartReturnsFalseAndClearsTheOutputs()
        {
            var graph = PathGraphFactory.FlatBlock(2, 2);
            var dijkstra = new HexDijkstra(graph);
            var nodes = new List<int> { 99 };
            var costs = new List<int> { 99 };

            Assert.IsFalse(dijkstra.TryFindReachable(Rules(), graph.NodeCount, 10, nodes, costs));
            Assert.IsEmpty(nodes);
            Assert.IsEmpty(costs);
        }

        [Test]
        public void ReachableSetAndCostsMatchTheBruteForceOracle()
        {
            var rules = Rules();
            var nodes = new List<int>();
            var costs = new List<int>();

            foreach (int seed in new[] { 2, 11, 99, 2024 })
            {
                var rng = new System.Random(seed);
                var graph = PathGraphFactory.Block(6, 6, (_, _) => rng.Next(0, 4));
                var dijkstra = new HexDijkstra(graph);

                for (int start = 0; start < graph.NodeCount; start += 7)
                {
                    int[] oracle = PathGraphFactory.ShortestCosts(graph, rules, start);
                    foreach (int budget in new[] { 0, 6, 14, int.MaxValue })
                    {
                        Assert.IsTrue(dijkstra.TryFindReachable(rules, start, budget, nodes, costs));

                        var got = new Dictionary<int, int>();
                        for (int i = 0; i < nodes.Count; i++)
                            got[nodes[i]] = costs[i];

                        for (int n = 0; n < graph.NodeCount; n++)
                        {
                            bool within = oracle[n] != int.MaxValue &&
                                          (budget == int.MaxValue || oracle[n] <= budget);
                            string where = $"seed {seed}, start {start}, budget {budget}, node {n}";
                            Assert.AreEqual(within, got.ContainsKey(n), where);
                            if (within)
                                Assert.AreEqual(oracle[n], got[n], where);
                        }
                    }
                }
            }
        }

        [Test]
        public void EveryReachableNodeCostAgreesWithHexAStar()
        {
            var rules = Rules();
            var rng = new System.Random(5);
            var graph = PathGraphFactory.Block(6, 6, (_, _) => rng.Next(0, 4));
            var dijkstra = new HexDijkstra(graph);
            var astar = new HexAStar(graph);
            var nodes = new List<int>();
            var costs = new List<int>();
            var path = new List<int>();
            var pathCosts = new List<int>();

            const int start = 0;
            dijkstra.TryFindReachable(rules, start, 14, nodes, costs);

            for (int i = 0; i < nodes.Count; i++)
            {
                Assert.IsTrue(astar.TryFindPath(rules, start, nodes[i], path, pathCosts), $"node {nodes[i]}");
                Assert.AreEqual(costs[i], pathCosts[^1], $"node {nodes[i]}");
            }
        }

        [Test]
        public void OneSolverServesRepeatedQueries()
        {
            var rules = Rules();
            var graph = PathGraphFactory.FlatBlock(5, 5);
            var dijkstra = new HexDijkstra(graph);
            var near = new List<int>();
            var all = new List<int>();

            Assert.IsTrue(dijkstra.TryFindReachable(rules, 0, 2, near, null));
            Assert.IsTrue(dijkstra.TryFindReachable(rules, 0, int.MaxValue, all, null));

            Assert.Less(near.Count, all.Count);
            Assert.AreEqual(graph.NodeCount, all.Count);   // a flat block is fully connected
        }
    }
}

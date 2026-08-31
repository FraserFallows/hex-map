using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace HexTerra.Tests
{
    public sealed class HexMathTests
    {
        private static readonly Vector3EqualityComparer V3 = new(1e-4f);

        // Every axial coord with q and r in [-range, range].
        private static IEnumerable<Vector2Int> Patch(int range)
        {
            for (int q = -range; q <= range; q++)
                for (int r = -range; r <= range; r++)
                    yield return new Vector2Int(q, r);
        }

        [Test]
        public void DirectionsHasSixEntries()
        {
            Assert.AreEqual(6, HexMath.Directions.Length);
        }

        [Test]
        public void EveryDirectionIsOneStepFromTheOrigin()
        {
            foreach (var dir in HexMath.Directions)
                Assert.AreEqual(1, HexMath.Distance(Vector2Int.zero, dir), $"{dir}");
        }

        [Test]
        public void DirectionsAreDistinct()
        {
            CollectionAssert.AllItemsAreUnique(HexMath.Directions);
        }

        [Test]
        public void DirectionsFormARingOfAdjacentSteps()
        {
            for (int d = 0; d < 6; d++)
            {
                var next = HexMath.Directions[(d + 1) % 6];
                Assert.AreEqual(1, HexMath.Distance(HexMath.Directions[d], next), $"index {d}");
            }
        }

        [Test]
        public void OppositeDirectionsCancel()
        {
            for (int d = 0; d < 6; d++)
                Assert.AreEqual(Vector2Int.zero, HexMath.Directions[d] + HexMath.Directions[(d + 3) % 6], $"index {d}");
        }

        [Test]
        public void CubeFormAlwaysSumsToZero()
        {
            foreach (var axial in Patch(8))
            {
                var cube = HexMath.ToCube(axial);
                Assert.AreEqual(0, cube.x + cube.y + cube.z, $"{axial}");
            }
        }

        [Test]
        public void AxialToCubeToAxialRoundTrips()
        {
            foreach (var axial in Patch(8))
                Assert.AreEqual(axial, HexMath.ToAxial(HexMath.ToCube(axial)), $"{axial}");
        }

        [Test]
        public void ToCubeMatchesTheKnownDerivation()
        {
            Assert.AreEqual(new Vector3Int(0, 0, 0), HexMath.ToCube(new Vector2Int(0, 0)));
            Assert.AreEqual(new Vector3Int(1, -1, 0), HexMath.ToCube(new Vector2Int(1, 0)));
            Assert.AreEqual(new Vector3Int(0, -1, 1), HexMath.ToCube(new Vector2Int(0, 1)));
            Assert.AreEqual(new Vector3Int(2, -5, 3), HexMath.ToCube(new Vector2Int(2, 3)));
            Assert.AreEqual(new Vector3Int(-4, 3, 1), HexMath.ToCube(new Vector2Int(-4, 1)));
        }

        [Test]
        public void DistanceToSelfIsZero()
        {
            foreach (var axial in Patch(6))
                Assert.AreEqual(0, HexMath.Distance(axial, axial), $"{axial}");
        }

        [Test]
        public void DistanceIsSymmetric()
        {
            foreach (var a in Patch(4))
                foreach (var b in Patch(4))
                    Assert.AreEqual(HexMath.Distance(a, b), HexMath.Distance(b, a), $"{a} {b}");
        }

        [Test]
        public void DistanceObeysTheTriangleInequality()
        {
            var probes = new[]
            {
                new Vector2Int(0, 0), new Vector2Int(3, -1), new Vector2Int(-2, 4),
                new Vector2Int(5, 5), new Vector2Int(-4, -3), new Vector2Int(1, -6)
            };

            foreach (var a in probes)
                foreach (var b in probes)
                    foreach (var c in probes)
                        Assert.LessOrEqual(HexMath.Distance(a, c),
                            HexMath.Distance(a, b) + HexMath.Distance(b, c), $"{a} {b} {c}");
        }

        [Test]
        public void DistanceMatchesTheKnownValues()
        {
            Assert.AreEqual(3, HexMath.Distance(new Vector2Int(0, 0), new Vector2Int(3, 0)));
            Assert.AreEqual(3, HexMath.Distance(new Vector2Int(0, 0), new Vector2Int(0, 3)));
            Assert.AreEqual(3, HexMath.Distance(new Vector2Int(0, 0), new Vector2Int(3, -3)));
            Assert.AreEqual(5, HexMath.Distance(new Vector2Int(0, 0), new Vector2Int(2, 3)));
            Assert.AreEqual(8, HexMath.Distance(new Vector2Int(-2, -2), new Vector2Int(2, 2)));
        }

        [Test]
        public void DistanceEqualsTheBreadthFirstHopCount()
        {
            var hops = BreadthFirstHops(new Vector2Int(0, 0), 5);
            foreach (var (coord, expected) in hops)
                Assert.AreEqual(expected, HexMath.Distance(Vector2Int.zero, coord), $"{coord}");
        }

        [Test]
        public void RoundLeavesIntegerCoordsUntouched()
        {
            foreach (var axial in Patch(6))
                Assert.AreEqual(axial, HexMath.Round(new Vector2(axial.x, axial.y)), $"{axial}");
        }

        [Test]
        public void RoundSnapsAPointInsideACellToThatCell()
        {
            Assert.AreEqual(new Vector2Int(3, -2), HexMath.Round(new Vector2(3.2f, -1.8f)));
            Assert.AreEqual(new Vector2Int(0, 0), HexMath.Round(new Vector2(-0.15f, 0.1f)));
            Assert.AreEqual(new Vector2Int(-4, 5), HexMath.Round(new Vector2(-3.9f, 4.85f)));
        }

        [Test]
        public void RoundReturnsTheNearestCellInCubeSpace()
        {
            var rng = new System.Random(12345);
            for (int i = 0; i < 400; i++)
            {
                var v = new Vector2((float)(rng.NextDouble() * 20 - 10), (float)(rng.NextDouble() * 20 - 10));
                var fractionalCube = new Vector3(v.x, -v.x - v.y, v.y);

                var result = HexMath.Round(v);
                float own = (CubeF(result) - fractionalCube).magnitude;

                foreach (var dir in HexMath.Directions)
                {
                    float neighbour = (CubeF(result + dir) - fractionalCube).magnitude;
                    Assert.LessOrEqual(own, neighbour + 1e-4f, $"{v} rounded to {result}");
                }
            }
        }

        [Test]
        public void OriginMapsToTheWorldOrigin()
        {
            Assert.That(HexMath.ToWorld(new Vector2Int(0, 0)), Is.EqualTo(Vector3.zero).Using(V3));
        }

        [Test]
        public void ToWorldStaysOnTheGroundPlane()
        {
            foreach (var axial in Patch(8))
                Assert.AreEqual(0f, HexMath.ToWorld(axial).y, 1e-6f, $"{axial}");
        }

        [Test]
        public void ToWorldIsLinearInTheCoordinate()
        {
            var a = new Vector2Int(1, 2);
            var b = new Vector2Int(3, -1);
            Assert.That(HexMath.ToWorld(a + b),
                Is.EqualTo(HexMath.ToWorld(a) + HexMath.ToWorld(b)).Using(V3));
        }

        [Test]
        public void AllSixNeighboursAreEquidistantInWorldSpace()
        {
            var centre = HexMath.ToWorld(new Vector2Int(2, -1));
            float expected = Mathf.Sqrt(3f);

            foreach (var dir in HexMath.Directions)
            {
                float gap = Vector3.Distance(centre, HexMath.ToWorld(new Vector2Int(2, -1) + dir));
                Assert.That(gap, Is.EqualTo(expected).Within(1e-4f), $"{dir}");
            }
        }

        [Test]
        public void FromWorldInvertsToWorld()
        {
            foreach (var axial in Patch(10))
                Assert.AreEqual(axial, HexMath.FromWorld(HexMath.ToWorld(axial)), $"{axial}");
        }

        [Test]
        public void FromWorldSnapsPointsWithinACellBackToItsCoord()
        {
            var rng = new System.Random(999);
            foreach (var axial in Patch(6))
            {
                var centre = HexMath.ToWorld(axial);
                for (int i = 0; i < 8; i++)
                {
                    var jitter = new Vector3((float)(rng.NextDouble() * 0.6 - 0.3), 0f,
                        (float)(rng.NextDouble() * 0.6 - 0.3));
                    Assert.AreEqual(axial, HexMath.FromWorld(centre + jitter), $"{axial} + {jitter}");
                }
            }
        }

        private static Vector3 CubeF(Vector2Int axial) => new(axial.x, -axial.x - axial.y, axial.y);

        // Shortest hop count from origin to every cell within maxHops, walking Directions only.
        private static IEnumerable<(Vector2Int coord, int hops)> BreadthFirstHops(Vector2Int origin, int maxHops)
        {
            var distances = new Dictionary<Vector2Int, int> { [origin] = 0 };
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(origin);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                int next = distances[current] + 1;
                if (next > maxHops)
                    continue;

                foreach (var dir in HexMath.Directions)
                {
                    var neighbour = current + dir;
                    if (distances.ContainsKey(neighbour))
                        continue;

                    distances[neighbour] = next;
                    frontier.Enqueue(neighbour);
                }
            }

            foreach (var pair in distances)
                yield return (pair.Key, pair.Value);
        }
    }
}

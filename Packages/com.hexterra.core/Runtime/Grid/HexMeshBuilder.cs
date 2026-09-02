using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexTerra
{
    /// <summary>
    /// Turns a populated HexGrid into geometry: one combined mesh per material on a single
    /// HexMapMesh GameObject, with per-hex surface data baked into every vertex.
    /// </summary>
    public class HexMeshBuilder
    {
        // Tops and walls share the surface material; edges reuse the wall mesh with their own.
        private readonly GameObject _hexTopPrefab;
        private readonly GameObject _hexWallPrefab;
        private readonly Material _hexSurfaceMaterial;
        private readonly Material _hexEdgeMaterial;
        private readonly Noise2D _variation;
        private readonly Transform _parent;

        private readonly float _variationScale;
        private readonly bool _perCellTint;
        private readonly float _tintOffsetX;
        private readonly float _tintOffsetY;
        private RectInt _bounds;

        private readonly List<CombineInstance> _surfaceInstances = new();
        private readonly List<CombineInstance> _edgeInstances = new();
        private readonly List<Mesh> _ownedMeshes = new();

        public HexMeshBuilder(GameObject topPrefab, GameObject wallPrefab, Material surfaceMaterial, Material edgeMaterial, TintConfig tint, int seed, Transform parent)
        {
            _hexTopPrefab = topPrefab;
            _hexWallPrefab = wallPrefab;
            _hexSurfaceMaterial = surfaceMaterial;
            _hexEdgeMaterial = edgeMaterial;
            _variation = tint.noise;
            _variationScale = Mathf.Max(tint.noiseScale, 0.0001f);
            _perCellTint = tint.perCell;
            _parent = parent;

            // Local RNG so the tint drift is reproducible for the seed without touching global state.
            var rng = new System.Random(seed);
            _tintOffsetX = (float)(rng.NextDouble() * 1000.0);
            _tintOffsetY = (float)(rng.NextDouble() * 1000.0);
        }

        public void Build(HexGrid grid)
        {
            _bounds = grid.AxialBounds;

            foreach (var hex in grid.HexArray)
            {
                if (!hex) continue;

                var cell = hex.GetComponent<HexCell>();
                int maxDrop = MaxDrop(cell);
                // Per-cell mode: one tint at the hex centre, shared by every vertex of the hex.
                float? cellTint = _perCellTint ? SampleTint(cell.transform.position) : null;
                GenerateTop(cell, maxDrop, cellTint);
                GenerateWalls(cell, maxDrop, cellTint);
            }

            BuildCombinedMesh();
        }

        // Largest downhill step from this cell to a real neighbour. The map boundary is not a
        // drop, so perimeter hexes score 0.
        private static int MaxDrop(HexCell cell)
        {
            int max = 0;
            foreach (var neighbour in cell.neighbours)
            {
                if (!neighbour) continue;

                int steps = Mathf.RoundToInt((cell.WorldHeight - neighbour.WorldHeight) / HexCell.StepMetres);
                if (steps > max) max = steps;
            }
            return max;
        }

        private void GenerateTop(HexCell cell, int maxDrop, float? cellTint)
        {
            AddCombineInstance(_surfaceInstances, _hexTopPrefab, cell.transform.position, Quaternion.identity, null,
                new VertexBake(cell.surfaceKind, 0, maxDrop, GridUV(cell), cellTint));
        }

        private void GenerateWalls(HexCell cell, int maxDrop, float? cellTint)
        {
            int orientation = 0;

            foreach (var neighbour in cell.neighbours)
            {
                float heightDiff = neighbour ? cell.WorldHeight - neighbour.WorldHeight : cell.WorldHeight;

                if (heightDiff > 0 || !neighbour)
                    AddWallInstances(cell, heightDiff, orientation, !neighbour, maxDrop, cellTint);

                orientation++;
            }
        }

        private void AddWallInstances(HexCell cell, float heightDiff, int orientation, bool isEdge, int maxDrop, float? cellTint)
        {
            var rotation = Quaternion.Euler(0, orientation * 60, 0);
            var instances = isEdge ? _edgeInstances : _surfaceInstances;
            int wallSteps = Mathf.RoundToInt(heightDiff / HexCell.StepMetres);

            // The wall's pivot sits at its top edge, so scaling localScale.y alone stretches
            // it downward to fill the height difference, no repositioning needed
            AddCombineInstance(instances, _hexWallPrefab, cell.transform.position, rotation, heightDiff,
                new VertexBake(cell.surfaceKind, wallSteps, maxDrop, GridUV(cell), cellTint));
        }

        // Briefly instantiates the prefab at the given world transform, captures a private
        // copy of its baked mesh for later combining, then destroys the instance immediately
        private void AddCombineInstance(List<CombineInstance> instances, GameObject prefab, Vector3 position, Quaternion rotation, float? scaleY, VertexBake bake)
        {
            var temp = Object.Instantiate(prefab, position, rotation);

            if (scaleY.HasValue)
                temp.transform.localScale = new Vector3(temp.transform.localScale.x, scaleY.Value, temp.transform.localScale.z);

            var meshFilter = temp.GetComponentInChildren<MeshFilter>();
            var mesh = Object.Instantiate(meshFilter.sharedMesh);

            if (scaleY.HasValue)
                WriteScaledUV3(mesh, scaleY.Value);

            BakeVertexData(mesh, meshFilter.transform.localToWorldMatrix, bake);

            instances.Add(new CombineInstance { mesh = mesh, transform = meshFilter.transform.localToWorldMatrix });
            _ownedMeshes.Add(mesh);

            Object.DestroyImmediate(temp);
        }

        // Per-hex data every combined vertex carries for the surface shader. wallSteps is 0 on
        // tops and selects the top/wall path; tint is per-vertex, or per-hex in per-cell mode.
        //   colour       = (surfaceKind / 2, tint, 0, 1)
        //   UV channel 1 = (gridU, gridV, wallSteps, hexMaxDrop)
        private void BakeVertexData(Mesh mesh, Matrix4x4 localToWorld, VertexBake bake)
        {
            var vertices = mesh.vertices;
            var colours = new Color[vertices.Length];
            var overlayUV = new List<Vector4>(vertices.Length);

            float kindChannel = (int)bake.Kind / 2f;

            for (int i = 0; i < vertices.Length; i++)
            {
                var world = localToWorld.MultiplyPoint3x4(vertices[i]);
                float tint = bake.CellTint ?? SampleTint(world);

                colours[i] = new Color(kindChannel, tint, 0f, 1f);
                overlayUV.Add(new Vector4(bake.GridUV.x, bake.GridUV.y, bake.WallSteps, bake.HexMaxDrop));
            }

            mesh.colors = colours;
            mesh.SetUVs(1, overlayUV);
        }

        // The variation field at a world XZ, scaled and seed-offset, clamped to [0, 1].
        // A missing field is a flat mid-grey.
        private float SampleTint(Vector3 world) => _variation != null
            ? Mathf.Clamp01(_variation.Sample(
                world.x / _variationScale + _tintOffsetX,
                world.z / _variationScale + _tintOffsetY))
            : 0.5f;

        // Copies UV0 into mesh.uv3 with V scaled by scaleY about the top of the layout, so a
        // tiled texture starts a clean tile at the wall's top edge whatever its height and the
        // remainder falls at the base, without disturbing UV0.
        private static void WriteScaledUV3(Mesh mesh, float scaleY)
        {
            var uvs = mesh.uv;

            float top = 0f;
            foreach (var uv in uvs)
                if (uv.y > top) top = uv.y;

            for (int i = 0; i < uvs.Length; i++)
                uvs[i].y = top - (top - uvs[i].y) * scaleY;

            mesh.uv3 = uvs;
        }

        // One submesh per material that produced anything, freeing the intermediate meshes as it goes.
        private void BuildCombinedMesh()
        {
            var combine = new List<CombineInstance>();
            var materials = new List<Material>();

            AddCombinedGroup(combine, materials, _surfaceInstances, _hexSurfaceMaterial);
            AddCombinedGroup(combine, materials, _edgeInstances, _hexEdgeMaterial);

            // Baked into the per-material meshes now. Release them so they don't leak as native objects.
            foreach (var mesh in _ownedMeshes)
                Object.DestroyImmediate(mesh);

            if (combine.Count == 0) return;

            var finalMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            finalMesh.CombineMeshes(combine.ToArray(), mergeSubMeshes: false, useMatrices: false);

            // Likewise, the per-material meshes are now baked into finalMesh
            foreach (var instance in combine)
                Object.DestroyImmediate(instance.mesh);

            var mapMesh = new GameObject("HexMapMesh");
            mapMesh.transform.SetParent(_parent, worldPositionStays: true);

            mapMesh.AddComponent<MeshFilter>().sharedMesh = finalMesh;
            mapMesh.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            mapMesh.AddComponent<MeshCollider>().sharedMesh = finalMesh;
        }

        private static void AddCombinedGroup(List<CombineInstance> combine, List<Material> materials, List<CombineInstance> group, Material material)
        {
            if (group.Count == 0) return;

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(group.ToArray(), mergeSubMeshes: true, useMatrices: true);

            combine.Add(new CombineInstance { mesh = mesh });
            materials.Add(material);
        }

        // Texel-centre coordinate of this cell in a bounds-sized overlay texture: (index + 0.5) / size.
        private Vector2 GridUV(HexCell cell) => new(
            (cell.q - _bounds.xMin + 0.5f) / _bounds.width,
            (cell.r - _bounds.yMin + 0.5f) / _bounds.height);

        public struct TintConfig
        {
            public Noise2D noise;
            public float noiseScale;
            public bool perCell;
        }

        private readonly struct VertexBake
        {
            public SurfaceKind Kind { get; }
            public int WallSteps { get; }
            public int HexMaxDrop { get; }
            public Vector2 GridUV { get; }

            // The hex's shared tint in per-cell mode; null when tint is sampled per vertex.
            public float? CellTint { get; }

            public VertexBake(SurfaceKind kind, int wallSteps, int hexMaxDrop, Vector2 gridUV, float? cellTint)
            {
                Kind = kind;
                WallSteps = wallSteps;
                HexMaxDrop = hexMaxDrop;
                GridUV = gridUV;
                CellTint = cellTint;
            }
        }
    }
}

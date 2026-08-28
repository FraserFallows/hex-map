using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexTerra
{
    public class HexGridManager
    {
        public GameObject[,] HexArray { get; private set; }

        /// <summary>
        /// Axial bounding box of the current grid. The array index for an axial coord is
        /// (q - AxialBounds.xMin, r - AxialBounds.yMin).
        /// </summary>
        public RectInt AxialBounds { get; private set; }

        /// <summary>
        /// The cell nearest the map's centre, or null if that cell falls outside the shape.
        /// </summary>
        public GameObject MidpointHex { get; private set; }

        // Mesh prefabs and materials for the hex top / wall — assigned by the consumer, never
        // loaded by path. Edges reuse the wall mesh with their own material.
        private readonly GameObject _hexTopPrefab;
        private readonly GameObject _hexWallPrefab;
        private readonly Material _hexTopMaterial;
        private readonly Material _hexWallMaterial;
        private readonly Material _hexEdgeMaterial;
        private readonly Transform _parent;

        private readonly List<CombineInstance> _topInstances = new();
        private readonly List<CombineInstance> _wallInstances = new();
        private readonly List<CombineInstance> _edgeInstances = new();
        private readonly List<Mesh> _ownedMeshes = new();

        private float _neighbourOrientation;
        private float _heightDifference;

        private const float StepScale = 0.25f;

        public HexGridManager(GameObject topPrefab, GameObject wallPrefab, Material topMaterial, Material wallMaterial, Material edgeMaterial, Transform parent)
        {
            _hexTopPrefab = topPrefab;
            _hexWallPrefab = wallPrefab;
            _hexTopMaterial = topMaterial;
            _hexWallMaterial = wallMaterial;
            _hexEdgeMaterial = edgeMaterial;
            _parent = parent;
        }

        public void SetGrid(GameObject[,] array, RectInt axialBounds)
        {
            HexArray = array;
            AxialBounds = axialBounds;
            MidpointHex = ComputeMidpointHex();
        }

        #region Initialisation
        public void InitialiseHexes()
        {
            foreach (var hex in HexArray)
            {
                if (!hex) continue;

                var hexStats = hex.GetComponent<HexCell>();

                // Step height must be applied before tops/walls are generated — they bake this
                // hex's current position into the combined mesh, so won't pick up later changes
                SetHexStepHeight(hex, hexStats);
                SetHexNeighbours(hexStats);
                GenerateTops(hexStats);
                GenerateWalls(hexStats);
            }

            BuildCombinedMesh();
        }

        private void SetHexStepHeight(GameObject hex, HexCell hexStats)
        {
            hex.transform.position = new Vector3(hex.transform.position.x, hexStats.stepHeight * StepScale, hex.transform.position.z);
        }

        private void SetHexNeighbours(HexCell cell)
        {
            for (int i = 0; i < HexMath.Directions.Length; i++)
            {
                var dir = HexMath.Directions[i];
                cell.neighbours[i] = GetHexAt(cell.q + dir.x, cell.r + dir.y);
            }
        }
        #endregion

        #region Mesh Generation
        private void GenerateTops(HexCell _hexStats)
        {
            AddCombineInstance(_topInstances, _hexTopPrefab, _hexStats.transform.position, Quaternion.identity, null);
        }

        private void GenerateWalls(HexCell _hexStats)
        {
            _neighbourOrientation = 0;

            foreach (var hexNeighbour in _hexStats.neighbours)
            {
                var neighbourStats = hexNeighbour ? hexNeighbour.GetComponent<HexCell>() : null;
                _heightDifference = neighbourStats ? (_hexStats.stepHeight - neighbourStats.stepHeight) * StepScale : _hexStats.stepHeight * StepScale;

                if (_heightDifference > 0 || !hexNeighbour)
                    AddWallInstances(_hexStats, _heightDifference, _neighbourOrientation, !hexNeighbour);

                _neighbourOrientation++;
            }
        }

        private void AddWallInstances(HexCell _hexStats, float heightDiff, float orientation, bool isEdge)
        {
            var rotation = Quaternion.Euler(0, orientation * 60, 0);
            var instances = isEdge ? _edgeInstances : _wallInstances;

            // The wall's pivot sits at its top edge, so scaling localScale.y alone stretches
            // it downward to fill the height difference — no repositioning needed
            AddCombineInstance(instances, _hexWallPrefab, _hexStats.transform.position, rotation, heightDiff);
        }

        // Briefly instantiates the prefab at the given world transform, captures a private
        // copy of its baked mesh for later combining, then destroys the instance immediately
        private void AddCombineInstance(List<CombineInstance> instances, GameObject prefab, Vector3 position, Quaternion rotation, float? scaleY)
        {
            var temp = Object.Instantiate(prefab, position, rotation);

            if (scaleY.HasValue)
                temp.transform.localScale = new Vector3(temp.transform.localScale.x, scaleY.Value, temp.transform.localScale.z);

            var meshFilter = temp.GetComponentInChildren<MeshFilter>();
            var mesh = Object.Instantiate(meshFilter.sharedMesh);

            if (scaleY.HasValue)
                WriteScaledUV3(mesh, scaleY.Value);

            instances.Add(new CombineInstance { mesh = mesh, transform = meshFilter.transform.localToWorldMatrix });
            _ownedMeshes.Add(mesh);

            Object.DestroyImmediate(temp);
        }

        // Copies UV0 into UV channel 2 (mesh.uv3), scaling V by scaleY — gives a stretched
        // piece a second, independently-tiling coordinate for a shader to read a repeating
        // texture from, without disturbing UV0
        private static void WriteScaledUV3(Mesh mesh, float scaleY)
        {
            var uvs = mesh.uv;
            for (int i = 0; i < uvs.Length; i++)
                uvs[i].y *= scaleY;
            mesh.uv3 = uvs;
        }

        // Merges the collected top/wall/edge instances into one mesh (one submesh per material
        // actually used), instead of leaving behind a separate GameObject per hex-top/wall-segment
        private void BuildCombinedMesh()
        {
            var combine = new List<CombineInstance>();
            var materials = new List<Material>();

            AddCombinedGroup(combine, materials, _topInstances, _hexTopMaterial);
            AddCombinedGroup(combine, materials, _wallInstances, _hexWallMaterial);
            AddCombinedGroup(combine, materials, _edgeInstances, _hexEdgeMaterial);

            // The per-piece meshes are now baked into the per-material meshes above and aren't
            // needed any more — release them so they don't linger as orphaned native mesh objects
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

        // Combines one material's worth of instances into a single mesh and, if it produced
        // anything, appends it (and its material) to the final per-material combine lists
        private static void AddCombinedGroup(List<CombineInstance> combine, List<Material> materials, List<CombineInstance> group, Material material)
        {
            if (group.Count == 0) return;

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(group.ToArray(), mergeSubMeshes: true, useMatrices: true);

            combine.Add(new CombineInstance { mesh = mesh });
            materials.Add(material);
        }
        #endregion

        #region Neighbours
        /// <summary>
        /// The hex at the given axial coordinates, or null if there's no generated map yet or
        /// the coordinates fall outside it.
        /// </summary>
        public GameObject GetHexAt(int q, int r)
        {
            if (HexArray == null) return null;

            int x = q - AxialBounds.xMin;
            int y = r - AxialBounds.yMin;
            if (x < 0 || x >= AxialBounds.width || y < 0 || y >= AxialBounds.height)
                return null;

            return HexArray[x, y];
        }

        private GameObject ComputeMidpointHex()
        {
            if (HexArray == null) return null;

            var centre = new Vector2(
                AxialBounds.xMin + (AxialBounds.width - 1) / 2f,
                AxialBounds.yMin + (AxialBounds.height - 1) / 2f);
            var axial = HexMath.Round(centre);
            return GetHexAt(axial.x, axial.y);
        }
        #endregion
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexTerra
{
    /// <summary>
    /// Turns a populated HexGrid into geometry: one combined mesh per material on a single
    /// HexMapMesh GameObject.
    /// </summary>
    public class HexMeshBuilder
    {
        // Edges reuse the wall mesh with their own material.
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

        public HexMeshBuilder(GameObject topPrefab, GameObject wallPrefab, Material topMaterial, Material wallMaterial, Material edgeMaterial, Transform parent)
        {
            _hexTopPrefab = topPrefab;
            _hexWallPrefab = wallPrefab;
            _hexTopMaterial = topMaterial;
            _hexWallMaterial = wallMaterial;
            _hexEdgeMaterial = edgeMaterial;
            _parent = parent;
        }

        public void Build(HexGrid grid)
        {
            foreach (var hex in grid.HexArray)
            {
                if (!hex) continue;

                var cell = hex.GetComponent<HexCell>();
                GenerateTop(cell);
                GenerateWalls(cell);
            }

            BuildCombinedMesh();
        }

        private void GenerateTop(HexCell cell)
        {
            AddCombineInstance(_topInstances, _hexTopPrefab, cell.transform.position, Quaternion.identity, null);
        }

        private void GenerateWalls(HexCell cell)
        {
            int orientation = 0;

            foreach (var neighbour in cell.neighbours)
            {
                float heightDiff = neighbour ? cell.WorldHeight - neighbour.WorldHeight : cell.WorldHeight;

                if (heightDiff > 0 || !neighbour)
                    AddWallInstances(cell, heightDiff, orientation, !neighbour);

                orientation++;
            }
        }

        private void AddWallInstances(HexCell cell, float heightDiff, int orientation, bool isEdge)
        {
            var rotation = Quaternion.Euler(0, orientation * 60, 0);
            var instances = isEdge ? _edgeInstances : _wallInstances;

            // The wall's pivot sits at its top edge, so scaling localScale.y alone stretches
            // it downward to fill the height difference, no repositioning needed
            AddCombineInstance(instances, _hexWallPrefab, cell.transform.position, rotation, heightDiff);
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

        // Copies UV0 into UV channel 2 (mesh.uv3), scaling V by scaleY. Gives a stretched
        // piece a second, independently-tiling coordinate for a shader to read a repeating
        // texture from, without disturbing UV0
        private static void WriteScaledUV3(Mesh mesh, float scaleY)
        {
            var uvs = mesh.uv;
            for (int i = 0; i < uvs.Length; i++)
                uvs[i].y *= scaleY;
            mesh.uv3 = uvs;
        }

        // One submesh per material that produced anything, freeing the intermediate meshes as it goes.
        private void BuildCombinedMesh()
        {
            var combine = new List<CombineInstance>();
            var materials = new List<Material>();

            AddCombinedGroup(combine, materials, _topInstances, _hexTopMaterial);
            AddCombinedGroup(combine, materials, _wallInstances, _hexWallMaterial);
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
    }
}

using UnityEngine;

namespace HexTerra
{
    public class HexMap : MonoBehaviour
    {
        public enum MapSize { Small, Medium, Large, ExtraLarge, Custom }

        // Hex prefab and materials handed to the package on generation
        [SerializeField] private GameObject hexPrefab;
        [SerializeField] private Material hexTopMaterial;
        [SerializeField] private Material hexWallMaterial;
        [SerializeField] private Material hexEdgeMaterial;
        // Elevation config handed to the package on generation
        [SerializeField] private FBMNoiseData noiseData;
        // Size preset; the edge count used when mapSize is Custom
        [SerializeField] private MapSize mapSize = MapSize.Medium;
        [SerializeField, Range(2, 200)] private int customMapSize = 20;

        [field: SerializeField] public bool AnimateOnPlay { get; set; }

        private HexMapGenerator _mapGenerator;
        private HexGridManager _hexGridManager;

        public void BeginGeneration()
        {
            ClearMap();
            _hexGridManager = new HexGridManager(hexTopMaterial, hexWallMaterial, hexEdgeMaterial, transform);
            _mapGenerator = new HexMapGenerator(hexPrefab, noiseData, EdgeCount(), _hexGridManager, transform);
            _mapGenerator.GenerateMap();
        }

        public void ClearMap()
        {
            while (transform.childCount > 0)
                foreach (Transform hex in transform)
                {
                    if (hex.gameObject)
                        DestroyImmediate(hex.gameObject);
                }
        }

        /// <summary>
        /// Returns the world position of the hex at the given grid coordinates, or null if
        /// the map hasn't been generated yet or the coordinates are out of range.
        /// </summary>
        public Vector3? GetHexWorldPosition(int x, int z)
        {
            var hex = _hexGridManager?.GetHexAt(x, z);
            return hex ? hex.transform.position : null;
        }

        /// <summary>
        /// The world position of the map's centre hex.
        /// </summary>
        public Vector3? GetMidpointWorldPosition()
        {
            var hex = _hexGridManager?.MidpointHex;
            return hex ? hex.transform.position : null;
        }
        
        private int EdgeCount() => mapSize switch
        {
            MapSize.Small => 10,
            MapSize.Medium => 20,
            MapSize.Large => 30,
            MapSize.ExtraLarge => 40,
            MapSize.Custom => customMapSize,
            _ => 20
        };
    }
}

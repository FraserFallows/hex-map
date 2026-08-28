using System;
using UnityEngine;
using UnityEngine.Events;

namespace HexTerra
{
    public enum MapShape { Hexagon, Rectangle, Parallelogram }
    public enum HeightmapSourceKind { Noise, Texture, Flat }

    public class HexMap : MonoBehaviour
    {
        // Hexagon reads width as hexes from centre to edge, so 2 spans 3 across; height is ignored.
        // Rectangle and Parallelogram use both.
        [SerializeField] private MapShape shape = MapShape.Hexagon;
        [SerializeField, Range(1, 400)] private int width = 20;
        [SerializeField, Range(1, 400)] private int height = 20;
        [SerializeField] private int seed;

        [SerializeField] private HeightmapSourceKind source = HeightmapSourceKind.Noise;
        [SerializeField] private NoisePreset noisePreset;
        [SerializeField] private Texture2D heightmapImage;
        [SerializeField] private int textureBands = 20;
        [SerializeField] private bool bilinear = true;
        [SerializeField] private int flatHeight;

        // Editor-only: an unsaved NoisePreset the inspector is tuning, used in place of the field
        // so noise can be previewed on the map before it is written to an asset.
        [NonSerialized] public NoisePreset noisePresetOverride;

        // Hex mesh prefabs and materials handed to the package on generation
        [SerializeField] private GameObject hexTopPrefab;
        [SerializeField] private GameObject hexWallPrefab;
        [SerializeField] private Material hexTopMaterial;
        [SerializeField] private Material hexWallMaterial;
        [SerializeField] private Material hexEdgeMaterial;

        /// <summary>
        /// Invoked after a build finishes, with the map as the argument. Wired in the Inspector so
        /// scene systems react without referencing HexMap in code.
        /// </summary>
        public MapGeneratedEvent mapGenerated = new();

        private HexMapGenerator _mapGenerator;
        private HexGridManager _hexGridManager;

        private NoisePreset ActiveNoisePreset => noisePresetOverride != null ? noisePresetOverride : noisePreset;

        // False when the chosen source is missing the asset it needs.
        public bool CanGenerate => source switch
        {
            HeightmapSourceKind.Noise => ActiveNoisePreset != null,
            HeightmapSourceKind.Texture => heightmapImage != null,
            _ => true
        };

        private void Start() => BeginGeneration();

        public void BeginGeneration()
        {
            if (!CanGenerate)
                return;

            ClearMap();
            _hexGridManager = new HexGridManager(hexTopPrefab, hexWallPrefab, hexTopMaterial, hexWallMaterial, hexEdgeMaterial, transform);

            _mapGenerator = new HexMapGenerator(CreateHeightmapSource(), CreateShape(), _hexGridManager, transform);
            _mapGenerator.GenerateMap();

            mapGenerated.Invoke(this);
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
        /// Returns the world position of the hex at the given axial coordinates, or null if
        /// the map hasn't been generated yet or the coordinates fall outside it.
        /// </summary>
        public Vector3? GetHexWorldPosition(int q, int r)
        {
            var hex = _hexGridManager?.GetHexAt(q, r);
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

        private IMapShape CreateShape() => shape switch
        {
            MapShape.Rectangle => new RectangleShape(width, height),
            MapShape.Parallelogram => new ParallelogramShape(width, height),
            _ => new HexagonShape(width - 1)
        };

        private IHeightmapSource CreateHeightmapSource() => source switch
        {
            HeightmapSourceKind.Texture => new TextureSource(heightmapImage, textureBands, bilinear),
            HeightmapSourceKind.Flat => new FlatSource(flatHeight),
            _ => new NoiseSource(ActiveNoisePreset.noise, ActiveNoisePreset.bands, ActiveNoisePreset.noiseScale, seed)
        };

        [Serializable]
        public class MapGeneratedEvent : UnityEvent<HexMap> { }
    }
}

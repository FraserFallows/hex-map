using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HexTerra
{
    public enum MapShape { Hexagon, Rectangle, Parallelogram }
    public enum HeightmapSourceKind { Noise, Texture, Flat }

    public class HexMap : MonoBehaviour
    {
        [Tooltip("Map outline. Hexagon uses Width only; Rectangle and Parallelogram use Width and Height.")]
        [SerializeField] private MapShape shape = MapShape.Hexagon;
        [Tooltip("Hexes per row.")]
        [SerializeField, Range(1, 200)] private int width = 80;
        [Tooltip("Hexes per column.")]
        [SerializeField, Range(1, 200)] private int height = 80;
        [Tooltip("Seeds noise sampling and the surface patch pattern. The same seed and recipe rebuild the same map.")]
        [SerializeField] private int seed;

        [Tooltip("Where per-cell step heights come from.")]
        [SerializeField] private HeightmapSourceKind source = HeightmapSourceKind.Noise;
        [Tooltip("Heightmap recipe sampled when Source is Noise.")]
        [SerializeField] private Heightmap heightmap;
        [Tooltip("Greyscale image read for step heights when Source is Texture. Needs Read/Write enabled in its import settings.")]
        [SerializeField] private Texture2D heightmapImage;
        [Tooltip("Discrete height levels the image's red channel is quantised into.")]
        [SerializeField] private int textureBands = 20;
        [Tooltip("Sample the image smoothly rather than nearest-texel when stretching it to the grid.")]
        [SerializeField] private bool bilinear = true;
        [Tooltip("Step height given to every cell when Source is Flat.")]
        [SerializeField] private int flatHeight;

        // Editor-only: an unsaved Heightmap the inspector is tuning, used in place of the field
        // so noise can be previewed on the map before it is written to an asset.
        [NonSerialized] public Heightmap heightmapOverride;

        [Tooltip("Gradient set that colours Grass / Dirt / Rock faces. Its wall-step bands also feed the classifier.")]
        [SerializeField] private SurfacePaletteSet surfacePalette;
        [Tooltip("Surface recipe: how slope, height, convexity and noise tag each face Grass, Dirt or Rock.")]
        [SerializeField] private Surfacemap surfacemap;

        // Editor-only working copy of surfacemap, the same pattern as heightmapOverride.
        [NonSerialized] public Surfacemap surfacemapOverride;

        [Tooltip("Prefab instantiated for each cell's top face.")]
        [SerializeField] private GameObject hexTopPrefab;
        [Tooltip("Prefab instantiated for the exposed sides of a raised cell.")]
        [SerializeField] private GameObject hexWallPrefab;
        [Tooltip("Material shared by cell tops and walls.")]
        [SerializeField] private Material hexSurfaceMaterial;
        [Tooltip("Material for the skirt around the map's outer edge.")]
        [SerializeField] private Material hexEdgeMaterial;

        /// <summary>
        /// Invoked after a build finishes, with the map as the argument. Wired in the Inspector so
        /// scene systems react without referencing HexMap in code.
        /// </summary>
        [Tooltip("Raised after each build completes, with this HexMap as the argument.")]
        public MapGeneratedEvent mapGenerated = new();

        /// <summary>
        /// Increments on every completed build. A cheap way for tooling to tell whether the
        /// current map is the one it last read.
        /// </summary>
        public int Generation { get; private set; }

        private HexGrid _grid;

        private Heightmap ActiveHeightmap => heightmapOverride ? heightmapOverride : heightmap;
        private Surfacemap ActiveSurfacemap => surfacemapOverride ? surfacemapOverride : surfacemap;

        // False until the render prefabs, the materials, and the chosen height source are all
        // assigned. Generation instantiates the prefabs, so a null one throws.
        public bool CanGenerate =>
            hexTopPrefab && hexWallPrefab && hexSurfaceMaterial && hexEdgeMaterial && source switch
            {
                HeightmapSourceKind.Noise => ActiveHeightmap != null,
                HeightmapSourceKind.Texture => heightmapImage != null,
                _ => true
            };

        /// <summary>
        /// Every cell in the current map, or empty if it has never been generated. Falls back to
        /// the live child cells when the transient grid was lost to a domain reload.
        /// </summary>
        public IReadOnlyList<HexCell> Cells => _grid?.Cells ?? GetComponentsInChildren<HexCell>();

        private void Start() => BeginGeneration();

        public void BeginGeneration()
        {
            if (!CanGenerate)
                return;

            ClearMap();

            var recipe = ActiveSurfacemap;

            var classifier = recipe
                ? new SurfaceClassifier(recipe.ToClassifierSettings(seed, surfacePalette ? surfacePalette.rockWallSteps : 0))
                : null;
            var generator = new HexMapGenerator(CreateHeightmapSource(), CreateShape(), transform, classifier);
            _grid = generator.Generate();

            var tint = recipe ? recipe.ToTintConfig() : default;
            var meshBuilder = new HexMeshBuilder(hexTopPrefab, hexWallPrefab, hexSurfaceMaterial, hexEdgeMaterial,
                tint, seed, transform);
            meshBuilder.Build(_grid);

            Generation++;
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
            var hex = _grid?.GetHexAt(q, r);
            return hex ? hex.transform.position : null;
        }

        /// <summary>
        /// The world position of the map's centre hex, or null if there's no map yet.
        /// </summary>
        public Vector3? GetMidpointWorldPosition()
        {
            var hex = _grid?.MidpointHex;
            return hex ? hex.transform.position : null;
        }

        private IHeightmapSource CreateHeightmapSource() => source switch
        {
            HeightmapSourceKind.Texture => new TextureSource(heightmapImage, textureBands, bilinear),
            HeightmapSourceKind.Flat => new FlatSource(flatHeight),
            _ => new NoiseSource(ActiveHeightmap.noise, ActiveHeightmap.maxHeight, ActiveHeightmap.noiseScale, seed)
        };

        private IMapShape CreateShape() => shape switch
        {
            MapShape.Rectangle => new RectangleShape(width, height),
            MapShape.Parallelogram => new ParallelogramShape(width, height),
            _ => new HexagonShape(width / 2)
        };

        [Serializable]
        public class MapGeneratedEvent : UnityEvent<HexMap> { }
    }
}

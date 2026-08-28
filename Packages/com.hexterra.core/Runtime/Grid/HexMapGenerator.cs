using UnityEngine;

namespace HexTerra
{
    public class HexMapGenerator
    {
        private readonly IHeightmapSource _heightmapSource;
        private readonly IMapShape _shape;
        private readonly HexGridManager _hexGridManager;
        private readonly Transform _parent;

        public HexMapGenerator(IHeightmapSource heightmapSource, IMapShape shape, HexGridManager hexGridManager, Transform parent)
        {
            _heightmapSource = heightmapSource;
            _shape = shape;
            _hexGridManager = hexGridManager;
            _parent = parent;
        }

        public void GenerateMap()
        {
            var bounds = _shape.AxialBounds;
            var array = new GameObject[bounds.width, bounds.height];

            // Offset every hex so the shape's bounding-box centre sits on the parent origin
            var centre = HexMath.ToWorld(
                bounds.xMin + (bounds.width - 1) / 2f,
                bounds.yMin + (bounds.height - 1) / 2f);

            foreach (var axial in _shape.Cells())
            {
                var hexGo = new GameObject($"Hex {axial.x},{axial.y}");
                hexGo.transform.SetParent(_parent);
                hexGo.transform.position = HexMath.ToWorld(axial) - centre;

                var cell = hexGo.AddComponent<HexCell>();
                cell.q = axial.x;
                cell.r = axial.y;

                array[axial.x - bounds.xMin, axial.y - bounds.yMin] = hexGo;
            }

            _hexGridManager.SetGrid(array, bounds);

            GenerateHeightmap(bounds);
            _hexGridManager.InitialiseHexes();
        }

        private void GenerateHeightmap(RectInt bounds)
        {
            if (_heightmapSource == null)
            {
                Debug.LogError("HexMapGenerator: no IHeightmapSource provided — cannot generate the heightmap.");
                return;
            }

            var heightmap = _heightmapSource.SampleHeightmap(bounds.width, bounds.height);

            for (int x = 0; x < bounds.width; x++)
            {
                for (int y = 0; y < bounds.height; y++)
                {
                    var hexObject = _hexGridManager.HexArray[x, y];
                    if (!hexObject) continue;

                    hexObject.GetComponent<HexCell>().stepHeight = heightmap[x, y];
                }
            }
        }
    }
}

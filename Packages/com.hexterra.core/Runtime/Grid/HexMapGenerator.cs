using UnityEngine;

namespace HexTerra
{
    /// <summary>
    /// Builds the logical grid and returns it as a HexGrid — a HexCell GameObject per shape cell,
    /// with the heightmap sampled onto them, each raised to its step height and its neighbours wired.
    /// </summary>
    public class HexMapGenerator
    {
        private readonly IHeightmapSource _heightmapSource;
        private readonly IMapShape _shape;
        private readonly Transform _parent;

        public HexMapGenerator(IHeightmapSource heightmapSource, IMapShape shape, Transform parent)
        {
            _heightmapSource = heightmapSource;
            _shape = shape;
            _parent = parent;
        }

        public HexGrid Generate()
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

            var grid = new HexGrid(array, bounds);

            ApplyHeightmap(grid);
            RaiseToStepHeight(grid);
            WireNeighbours(grid);

            return grid;
        }

        private void ApplyHeightmap(HexGrid grid)
        {
            if (_heightmapSource == null)
            {
                Debug.LogError("HexMapGenerator: no IHeightmapSource provided — cannot generate the heightmap.");
                return;
            }

            var bounds = grid.AxialBounds;
            var heightmap = _heightmapSource.SampleHeightmap(bounds.width, bounds.height);

            for (int x = 0; x < bounds.width; x++)
            {
                for (int y = 0; y < bounds.height; y++)
                {
                    var hex = grid.HexArray[x, y];
                    if (!hex) continue;

                    hex.GetComponent<HexCell>().stepHeight = heightmap[x, y];
                }
            }
        }

        private static void RaiseToStepHeight(HexGrid grid)
        {
            foreach (var hex in grid.HexArray)
            {
                if (!hex) continue;

                var position = hex.transform.position;
                hex.transform.position = new Vector3(position.x, hex.GetComponent<HexCell>().WorldHeight, position.z);
            }
        }

        private static void WireNeighbours(HexGrid grid)
        {
            foreach (var hex in grid.HexArray)
            {
                if (!hex) continue;

                var cell = hex.GetComponent<HexCell>();
                for (int i = 0; i < HexMath.Directions.Length; i++)
                {
                    var dir = HexMath.Directions[i];
                    cell.neighbours[i] = grid.GetHexAt(cell.q + dir.x, cell.r + dir.y);
                }
            }
        }
    }
}

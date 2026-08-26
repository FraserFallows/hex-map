using System;
using UnityEngine;

namespace HexTerra
{
    public class HexMapGenerator
    {
        /// <summary>
        /// Raised once GenerateMap() has finished building and elevating the grid.
        /// </summary>
        public event Action MapGenerated;

        private readonly GameObject _hex;
        private readonly FBMNoiseData _noiseData;
        private readonly int _edgeCount;
        private readonly HexGridManager _hexGridManager;
        private readonly Transform _parent;
        // Number of hexes along diameter
        private int _diameter;
        // Lower and upper limits of the z-coordinate for the current column
        private int _zLowerLimit, _zUpperLimit;
        private int _x, _z, _xGridPos, _zGridPos;
        // Sign to determine the direction of grid positions
        private int _nSign;
        private float _zPos;

        private const float XOffset = 0.75f;
        private const float ZOffset = 0.866025404f;

        public HexMapGenerator(GameObject hexPrefab, FBMNoiseData noiseData, int edgeCount, HexGridManager hexGridManager, Transform parent)
        {
            _hex = hexPrefab;
            _noiseData = noiseData;
            _edgeCount = edgeCount;
            _hexGridManager = hexGridManager;
            _parent = parent;
        }

        public void GenerateMap()
        {
            var total = 0;

            _zLowerLimit = 0;
            _zUpperLimit = _diameter = (_edgeCount * 2) - 1;

            var tempHexArray = new GameObject[_diameter, _diameter];

            for (_x = 0; _x < _edgeCount; _x++)
            {
                // Adjust upper and lower limits based on column parity
                if (_x % 2 == 1)
                    _zUpperLimit--;
                else if (_x > 1)
                    _zLowerLimit++;

                // Hex map generation - starts with the centre column
                // Generates a further two columns on either side repeatedly until done
                for (_z = _zLowerLimit; _z < _zUpperLimit; _z++)
                {
                    _zPos = _z * ZOffset;

                    if (_x % 2 == 1)
                        _zPos += ZOffset / 2;

                    for (int i = -1; i < 2; i++)
                    {
                        if (i == 0) continue;

                        _nSign = i;
                        if (_x == 0)
                            _nSign = 0;

                        CalculateGridPositions(_x, _z, out _xGridPos, out _zGridPos);

                        var hexGo = UnityEngine.Object.Instantiate(_hex, new Vector3(_nSign * (_x * 2 * XOffset), 0, _zPos * 2), Quaternion.identity);
                        hexGo.name = $"Hex_{_x * _nSign}_{_z} : {_xGridPos} {_zGridPos}";

                        var hexGoStats = hexGo.GetComponent<HexCell>();
                        hexGoStats.xGridPos = _xGridPos;
                        hexGoStats.zGridPos = _zGridPos;

                        hexGo.transform.SetParent(_parent);

                        tempHexArray[_xGridPos, _zGridPos] = hexGo;

                        total++;

                        if (_x == 0)
                            break;
                    }
                }
            }
            Debug.Log(total);

            _hexGridManager.HexArray = tempHexArray;

            GenerateElevation();
            _hexGridManager.InitialiseHexes();

            MapGenerated?.Invoke();
        }

        private void GenerateElevation()
        {
            if (!_noiseData)
            {
                Debug.LogError("HexMapGenerator: no FBMNoiseData assigned — cannot generate elevation.");
                return;
            }

            // v-TODO: SETUP A RANDOM SEED - REPLACE THIS LINE WITH IT-v
            var seed = UnityEngine.Random.Range(0, int.MaxValue);
            UnityEngine.Random.InitState(seed);

            var translation = new Vector2(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));

            IElevationSource elevationSource = new FBMElevationSource(_noiseData);
            var stepHeights = elevationSource.SampleElevation(_diameter, translation);

            // Apply displacement to hexes based on the sampled elevation
            for (int x = 0; x < _diameter; x++)
            {
                for (int y = 0; y < _diameter; y++)
                {
                    var hexObject = _hexGridManager.HexArray[x, y];
                    if (!hexObject) continue;

                    var hexStats = hexObject.GetComponent<HexCell>();
                    hexStats.stepHeight = stepHeights[x, y];
                }
            }
        }

        private void CalculateGridPositions(int x, int z, out int xGridPos, out int zGridPos)
        {
            xGridPos = x * _nSign + _edgeCount - 1;

            if (Mathf.Abs(x * _nSign) % 2 == 1 && Mathf.Abs(x * _nSign) > 0)
                zGridPos = z + 1;
            else
                zGridPos = z;
        }
    }
}

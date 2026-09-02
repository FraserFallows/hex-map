#if UNITY_EDITOR
using UnityEngine;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// Scene-view preview for a Pathfinder: click a hex to set the start, shift-click for the goal,
    /// or drag either. Editor only.
    /// </summary>
    [RequireComponent(typeof(Pathfinder))]
    public sealed class PathfindingVisualiser : MonoBehaviour
    {
        public Vector2Int start;
        public Vector2Int goal;
        public bool drawPath = true;
        public bool drawStepCosts = true;
        public bool drawReachable = true;
    }
}
#endif

#if UNITY_EDITOR
using UnityEngine;

namespace HexTerra.Pathfinding
{
    /// <summary>
    /// Scene-view preview of a Pathfinder route. Select it, then click a hex to set the start and
    /// shift-click for the goal; either endpoint can also be dragged. Editor only.
    /// </summary>
    [RequireComponent(typeof(Pathfinder))]
    public sealed class PathfindingVisualiser : MonoBehaviour
    {
        public Vector2Int start;
        public Vector2Int goal;
        public bool drawStepCosts = true;
    }
}
#endif

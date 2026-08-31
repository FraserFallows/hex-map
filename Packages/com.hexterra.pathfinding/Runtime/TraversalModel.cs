namespace HexTerra.Pathfinding
{
    /// <summary>
    /// Movement rules over a PathGraph. A move costs baseCost plus a band looked up by the height
    /// change in HexCell steps: ascentCost for a rise, descentCost for a drop, index 0 for flat.
    /// A change past the end of a table is impassable.
    /// </summary>
    public readonly struct TraversalModel
    {
        public readonly int baseCost;
        public readonly int[] ascentCost;
        public readonly int[] descentCost;

        public TraversalModel(int baseCost, int[] ascentCost, int[] descentCost)
        {
            this.baseCost = baseCost;
            this.ascentCost = ascentCost;
            this.descentCost = descentCost;
        }

        public int MaxClimb => ascentCost.Length - 1;
        public int MaxDrop => descentCost.Length - 1;

        /// <summary>
        /// True when the height change between neighbours falls inside the tables: a rise shorter
        /// than ascentCost, a drop shorter than descentCost.
        /// </summary>
        public bool CanEnter(int fromStepHeight, int toStepHeight)
        {
            int delta = toStepHeight - fromStepHeight;
            return delta >= 0 ? delta < ascentCost.Length : -delta < descentCost.Length;
        }

        /// <summary>
        /// Cost of one move between neighbours: baseCost plus the band for the height change.
        /// Only meaningful where CanEnter holds for the same pair.
        /// </summary>
        public int MoveCost(int fromStepHeight, int toStepHeight)
        {
            int delta = toStepHeight - fromStepHeight;
            return baseCost + (delta >= 0 ? ascentCost[delta] : descentCost[-delta]);
        }
    }
}

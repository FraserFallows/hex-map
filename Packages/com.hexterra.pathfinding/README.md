# HexTerra Pathfinding

Pathfinding for [HexTerra](../com.hexterra.core) hex maps: A* routes and cost-bounded reachability. Depends on `com.hexterra.core`; nothing in core depends back on it.

## Contents

- **`PathGraph`** — an immutable snapshot of a generated map, built once per generation: one node per cell, addressed by dense index, carrying axial coord, step height, the six neighbour indices (`-1` at the map edge) and world position.
- **`PathGraphBuilder`** — `Build(IReadOnlyList<HexCell>)`, fed from `HexMap.Cells`.
- **`TraversalModel`** — the movement rules, in whole points: `baseCost` plus `ascentCost` / `descentCost` band tables, indexed by the height change in `HexCell` steps. Each table's length is its reach; a change past the end is impassable. The caller builds it from its own serialised config.
- **`HexAStar`** — the point-to-point solver. Integer costs throughout. Bound to one `PathGraph`, reuses its buffers across queries (so not thread-safe). The heuristic is grid distance times `baseCost`, admissible and consistent while every band is non-negative. `TryFindPath` fills a caller-supplied `List<int>` (and an optional `costOut` with the cumulative cost per node) and returns `false` with the lists cleared when there is no route.
- **`HexDijkstra`** — the reachability query: cost-bounded Dijkstra from one node, filling a caller-supplied `List<int>` with every node reachable for at most a given budget, in ascending cost order (and an optional costs list at matching indices). Same one-graph, buffer-reusing, not-thread-safe contract as `HexAStar`.
- **`Pathfinder`** — the MonoBehaviour entry point. Serialised `baseCost`, the two band tables, and a default `movePoints` budget for cost-bounded queries. Wire `RebuildGraph` to `HexMap.mapGenerated`; call `TryFindPath` between two axial coords, or `TryFindReachable` from one.
- **`PathfindingVisualiser`** — editor-only debug component. Click a hex for the start, shift-click for the goal (or drag either); the scene view draws the route with per-step and total cost, and outlines the set reachable from the start within one `movePoints` budget.

## Status

Pre-1.0. The API isn't stable yet.

## Requires

`com.hexterra.core` 0.3.0 or newer.

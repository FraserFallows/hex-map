# HexTerra

Procedural hexagonal grid and terrain generation for Unity: coordinate/neighbour math, mesh and wall generation, and pluggable elevation sampling.

## What's in here

- **Grid** — hex coordinate/neighbour math (`HexCoordinateMatrix`), per-cell data (`HexCell`), grid assembly and mesh/wall generation (`HexGridManager`, `HexMapGenerator` — plain C# classes, constructed and owned by `HexMap`), and `HexMap` — the single entry point a consumer adds to a GameObject to drive the above
- **Elevation** — `IElevationSource`, with two implementations: `FBMElevationSource` (fractal Brownian motion noise, configured via an `FBMNoiseData` asset) and `FlatElevationSource` (constant height — a baseline, or handy for tests)
- **Content** — the default hex meshes, materials, shader graphs, and prefabs the grid instantiates out of the box (`Runtime/Grid/Content/`)

## Usage

Add a `HexMap` to a GameObject, assign a hex prefab (see `Runtime/Grid/Content/Prefabs/Hex.prefab`), the three hex materials, and an `FBMNoiseData` asset, then call `BeginGeneration()`. `HexMap` constructs and wires up `HexMapGenerator` and `HexGridManager` internally, and `GenerateMap()` raises a `MapGenerated` event once the grid is built and elevated.

The package never reaches into your scene by name and never loads anything via `Resources.Load` — everything it needs is handed to it as a serialized field or constructor parameter.

## Status

Early (`0.1.0`) — pulled out of a larger project as its own package. API isn't stable yet.

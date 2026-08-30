# HexTerra

Procedural hexagonal grid and terrain generation for Unity: coordinate/neighbour math, mesh and wall generation, and pluggable heightmap sampling.

## What's in here

- **Map**: `HexMap`, a `MonoBehaviour` holding the whole recipe: shape + size, `seed`, one heightmap source (noise / texture / flat) with its config, and the mesh/material wiring. It builds itself on `Start` (or on demand via `BeginGeneration()`) and invokes its `mapGenerated` UnityEvent (passing itself) when done; the same source config and the same `seed` always produce the same map.
- **Grid**: axial hex math (`Hex`: neighbour directions, cube conversion, distance, rounding), per-cell data (`HexCell`), map outlines (`IMapShape` with `HexagonShape` / `RectangleShape` / `ParallelogramShape`), and grid assembly + mesh/wall generation (`HexGridManager` and `HexMapGenerator`, plain C# classes owned by `HexMap`)
- **Heightmap**: `IHeightmapSource`, with three implementations built by `HexMap` from its serialised settings: `NoiseSource` (quantises a `Noise2D` field, deterministic for a seed), `TextureSource` (reads the red channel of an image), and `FlatSource` (constant height). `HeightmapConverter` bridges both directions: `Render` bakes a `Noise2D` to a texture, `Read` samples any image into an `int[,]` of step-heights.
- **Noise**: `Noise2D`, a `[SerializeReference]` polymorphic tree, all outputting [0, 1], composed inline on a `NoisePreset`, a reusable ScriptableObject (`Hills`, `Mountains`, `Steppe`…) that bundles the tree with its max height and feature size, which a `HexMap` points at and several maps can share. Primitives: `PerlinNoise`, `SimplexNoise`, `WorleyNoise`, each with a `scale` setting its feature size. Combiners that wrap other `Noise2D`s: `FractalNoise` (`Fbm` / `Ridged` / `Billow` octave stacking), `DomainWarpNoise` (offsets a source's sample position by a warp noise), `CurveNoise` (remaps a source through an `AnimationCurve`: invert, contrast), `TerraceNoise` (quantises a source into flat plateaus separated by ramps), `RemapNoise` (stretches a source's mid-range window to fill [0, 1]), `SmoothNoise` (box-blurs a source to soften its shape), and `LayeredNoise` (folds a list of sources together with per-layer blend mode, weight, and optional mask).
- **Content**: the default hex meshes, materials, shader graphs, and prefabs the grid instantiates out of the box (`Content/`)

## Usage

Add a `HexMap` to a GameObject, assign the hex top and wall mesh prefabs (see `Content/Prefabs/`) and the three hex materials, set the shape / size / seed, and pick a heightmap source. For the noise source, assign a `NoisePreset` (Create → HexTerra → Noise Preset); build a small library of them (`Hills`, `Mountains`, `Steppe`) and point maps at whichever you want. On `Start` (or a `BeginGeneration()` call) `HexMap` builds `HexMapGenerator` and `HexGridManager` from its settings and invokes its `mapGenerated` UnityEvent (passing itself) once the grid is built and its heightmap applied. Wire reactions into it in the Inspector, or `AddListener` from code (frame a camera, place a light, hook pathfinding).

In the editor the `HexMap` inspector embeds the noise fields inline, including the baked preview, and you always edit a working copy: it previews live on the map (with "Auto-regenerate on change" enabled, rebuilding on field commit, not mid-keystroke) but only reaches the preset asset when you press **Save**. Deselecting the HexMap discards unsaved edits and reloads the asset. With **no** preset assigned, **Save Noise Preset…** writes a new asset into the package `Data` folder; **New** starts a fresh unsaved noise, **Duplicate** forks the assigned one.

A texture heightmap needs Read/Write enabled in its import settings; for correct heights also disable sRGB (uncheck "sRGB (Color Texture)") and set compression to None. Do any level / contrast / invert edits on the image before import.

The package never reaches into your scene by name and never loads anything via `Resources.Load`: everything it needs is handed to it as a serialized field or constructor parameter.

## Status

Early. Pulled out of a larger project as its own package. API isn't stable yet.

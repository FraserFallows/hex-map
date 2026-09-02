# Changelog

## [0.3.0] - 2026-09-02

The surface layer: per-cell Grass / Dirt / Rock classification baked into the mesh, a palette-driven `HexSurface` shader, and reusable `Heightmap` and `Surfacemap` recipe assets edited through a reworked `HexMap` inspector.

### New

- Added `HexMap.Cells` and `HexGrid.Cells`. The map's cells as an `IReadOnlyList<HexCell>`.
- Added `SurfaceKind` and a surface classifier. Tags each cell `Grass`, `Dirt` or `Rock` from its slope, altitude, convexity and a noise field, with majority-filter cleanup passes, then forces `Rock` on any cell that drops a palette-set number of steps to a neighbour. Exposed as `HexCell.surfaceKind`, configured on `HexMap`.
- Baked per-hex surface data into the combined mesh. Each vertex carries its cell's `surfaceKind`, wall height data, a grid-coordinate UV and a tint value from the `Surfacemap` tint noise, in vertex colour and UV1.
- Added `SurfacePaletteSet` and the `HexSurface` shader. Per-`SurfaceKind` tint gradients bake into a lookup the shader samples by the baked vertex tint value. A wall's kind is the higher of its cell's kind and a height band set by `dirtWallSteps` / `rockWallSteps`.
- Added a feathered cell outline to the `HexSurface` shader (`CellBorder`, `HexOutlineDistance`, `BoxOutlineDistance` sub-graphs). A signed-distance border traces the hex edges on tops and the wall-quad edges on walls. `_BorderThickness`, `_BorderFeather` and `_BorderColour` style it, and `_WallWidth`, `_CellApothem` and `_StepMetres` supply the cell dimensions.
- Added `Surfacemap`, a reusable surface recipe: the classifier noise / scale / weights / whole-step references / thresholds / cleanup passes, plus a `tint` block (noise, scale, and a `perCell` toggle that colours each cell one flat tint instead of drifting per vertex). Same New / Duplicate / Overwrite / Save as New / Reset panel as `Heightmap`. Ships a `RockyHills_Grass01` instance in `Data/Surfacemaps/`.
- Added the `Rocky Plains` heightmap preset.
- Added `HexMap` inspector previews: a render of the heightmap noise; a map of the last generated grid, palette-tinted when a `SurfacePaletteSet` is assigned; and the surface recipe run straight over the heightmap noise, kinds beside the classifier noise and palette-tinted (tint drift and a height shade) beside the tint noise, both refreshed without regenerating.
- Added `OneMinusNoise`. Inverts a source, mapping 0 to 1 and 1 to 0.
- Added `FlatNoise`. A constant field at a chosen value, for use as a floor or bias term inside `LayeredNoise`.
- Added a nesting accent to the `Noise2D` inspector. Each node's container draws a coloured left bar that advances through a small palette as sources nest, so a deep stack of the same node type reads apart.
- Added an inspector tooltip to every `Noise2D`, `Heightmap`, `Surfacemap` and `SurfacePaletteSet` field.
- Added `HexMap.Generation`, a counter that increments on each completed build.

### Changes

- Renamed `NoisePreset` to `Heightmap`. Existing assets resolve by script GUID, but serialized `HexMap` references do not carry over: re-assign the `heightmap` field (was `noisePreset`), and update any code that names the type.
- Moved surface classification and the vertex-tint noise off `HexMap`'s inline fields onto the new `Surfacemap` asset. `HexMap` now takes a `surfacemap` reference plus a bare `surfacePalette`. Re-assign both on an existing map. The `HexMap` inspector is split into labelled zones (Map, Heightmap, Surfacemap, Rendering, Events).
- Hexagon maps read `width` as the hex count across the middle, like Rectangle and Parallelogram. It was the centre-to-edge count, so a Hexagon was about twice the width of the others at the same value. Even values round up to the next odd. Halve `width` on existing Hexagon maps.
- Merged `HexMap.hexTopMaterial` and `hexWallMaterial` into one `hexSurfaceMaterial`. Tops and walls render from the same material; re-assign it on existing maps.
- `HexCell.neighbours` is a `HexCell[]` now, not a `GameObject[]`. Code reading it no longer needs `GetComponent<HexCell>()`.
- Renamed the Package Manager entry to `HexTerra Core`. The `com.hexterra.core` id is unchanged.
- Lowered the minimum Unity version to 2022.3 and declared the `com.unity.test-framework` dependency.

### Fixes

- Auto-regenerate triggers only on `HexMap`, `Heightmap` and `Surfacemap` edits. It was rebuilding on any component change in the scene.
- `Noise2D` inspector node foldouts drew their arrow in the inspector's left gutter, outside the node box. It now sits in the node header.
- The preset panel's `Overwrite` and `Save as New` wrote the asset during the inspector's layout pass, logging a recursive-layout warning. They now defer the write past the pass, matching `New`, `Duplicate` and `Reset`.

## [0.2.0] - 2026-08-30

A pass over the noise system: new shaping and layering nodes, a rebuilt inspector for the graph.

### New

- Added `TerraceNoise`. Snaps a field to a set number of flat heights with a ramp between each.
- Added `RemapNoise`. Stretches a chosen input window to fill `[0, 1]`, with a "Calibrate to source range" button that reads the actual range off the source.
- Added `SmoothNoise`. A box blur that softens a field's shape rather than its values.
- Added `LayeredNoise`. Folds a list of sources together, each with its own blend mode, weight and optional mask.
- Added a `scale` field to `PerlinNoise`, `SimplexNoise` and `WorleyNoise`, so each generator sets its own feature size.
- Added `FractalNoise.octaveRotation`. Turns each octave a few degrees past the last so their axis-aligned artefacts cancel instead of stacking into streaks. Defaults to 30.
- Rebuilt the `Noise2D` inspector. Type picker on every slot, `LayeredNoise` layers as a reorderable list, an insert/remove menu for slipping a combiner above a node, and each node boxed with its type as the heading.
- Added the `Steppe` preset, built on the new nodes.

### Changes

- Split `bands` in two. `NoisePreset.maxHeight` sets the tallest a cell can be, and `HeightmapConverter.Render`'s `steps` sets the preview's grey levels.
- Reworked how the `HexMap` inspector saves noise edits: Overwrite, Save as New and Reset. Edits now survive an inspector rebuild.
- `FractalNoise` presets render differently. `octaveRotation` stops octave artefacts stacking into streaks.

### Fixes

- `PerlinNoise` clamps its output to `[0, 1]`. `Mathf.PerlinNoise` can drift slightly past the edges.

## [0.1.0]

Initial release. Hex grid and terrain generation, pluggable heightmap sources, and a composable `Noise2D` graph.
